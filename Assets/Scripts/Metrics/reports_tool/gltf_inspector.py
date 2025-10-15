#!/usr/bin/env python3
"""
GLTF Inspector - Extrai informações técnicas de arquivos GLB/GLTF

Este script inspeciona arquivos GLB e extrai:
- Informações do header (versão, tamanho)
- Contagem de nós, meshes, materiais, texturas
- Estimativa de vértices e triângulos
- Informações de accessors e buffers
"""

import struct
import json
import sys
import os

def inspect_gltf_file(glb_path):
    """
    Extrai informações técnicas de um arquivo GLB
    
    Args:
        glb_path: Caminho para o arquivo GLB
        
    Returns:
        Dict com informações ou None em caso de erro
    """
    
    try:
        if not os.path.exists(glb_path):
            print(f"[gltf_inspector] Arquivo não encontrado: {glb_path}", file=sys.stderr)
            return None
            
        file_size = os.path.getsize(glb_path)
        
        with open(glb_path, 'rb') as f:
            # Ler header GLB (12 bytes)
            magic = f.read(4)
            if magic != b'glTF':
                print(f"[gltf_inspector] Arquivo não é GLB válido (magic: {magic})", file=sys.stderr)
                return None
            
            version = struct.unpack('<I', f.read(4))[0]
            length = struct.unpack('<I', f.read(4))[0]
            
            # Ler chunk JSON (primeiro chunk)
            chunk_length = struct.unpack('<I', f.read(4))[0]
            chunk_type = f.read(4)
            
            if chunk_type != b'JSON':
                print(f"[gltf_inspector] Primeiro chunk não é JSON: {chunk_type}", file=sys.stderr)
                return None
            
            # Ler JSON data
            json_data = f.read(chunk_length).decode('utf-8')
            gltf_json = json.loads(json_data)
            
            # Extrair informações
            info = {
                "file_path": glb_path,
                "file_size": file_size,
                "version": version,
                "length": length,
                "nodes": len(gltf_json.get('nodes', [])),
                "meshes": len(gltf_json.get('meshes', [])),
                "materials": len(gltf_json.get('materials', [])),
                "textures": len(gltf_json.get('textures', [])),
                "images": len(gltf_json.get('images', [])),
                "samplers": len(gltf_json.get('samplers', [])),
                "accessors": len(gltf_json.get('accessors', [])),
                "bufferViews": len(gltf_json.get('bufferViews', [])),
                "buffers": len(gltf_json.get('buffers', [])),
                "vertex_count": estimate_vertex_count(gltf_json),
                "triangle_count": estimate_triangle_count(gltf_json),
                "has_normals": has_attribute(gltf_json, 'NORMAL'),
                "has_texcoords": has_attribute(gltf_json, 'TEXCOORD_0'),
                "has_tangents": has_attribute(gltf_json, 'TANGENT'),
                "has_colors": has_attribute(gltf_json, 'COLOR_0'),
                "extensions_used": gltf_json.get('extensionsUsed', []),
                "extensions_required": gltf_json.get('extensionsRequired', [])
            }
            
            return info
    
    except Exception as e:
        print(f"[gltf_inspector] Erro ao inspecionar {glb_path}: {e}", file=sys.stderr)
        import traceback
        traceback.print_exc(file=sys.stderr)
        return None


def estimate_vertex_count(gltf_json):
    """
    Estima número total de vértices baseado em accessors
    
    Args:
        gltf_json: Dict com dados GLTF
        
    Returns:
        Int com estimativa de vértices
    """
    try:
        meshes = gltf_json.get('meshes', [])
        accessors = gltf_json.get('accessors', [])
        
        total_vertices = 0
        
        for mesh in meshes:
            for primitive in mesh.get('primitives', []):
                attributes = primitive.get('attributes', {})
                
                # POSITION accessor sempre existe e indica o número de vértices
                if 'POSITION' in attributes:
                    position_accessor_idx = attributes['POSITION']
                    if position_accessor_idx < len(accessors):
                        accessor = accessors[position_accessor_idx]
                        total_vertices += accessor.get('count', 0)
        
        return total_vertices
    
    except Exception as e:
        print(f"[gltf_inspector] Erro ao estimar vértices: {e}", file=sys.stderr)
        return 0


def estimate_triangle_count(gltf_json):
    """
    Estima número total de triângulos baseado em indices ou vértices
    
    Args:
        gltf_json: Dict com dados GLTF
        
    Returns:
        Int com estimativa de triângulos
    """
    try:
        meshes = gltf_json.get('meshes', [])
        accessors = gltf_json.get('accessors', [])
        
        total_triangles = 0
        
        for mesh in meshes:
            for primitive in mesh.get('primitives', []):
                mode = primitive.get('mode', 4)  # 4 = TRIANGLES (default)
                
                # Se tem índices
                if 'indices' in primitive:
                    indices_accessor_idx = primitive['indices']
                    if indices_accessor_idx < len(accessors):
                        accessor = accessors[indices_accessor_idx]
                        index_count = accessor.get('count', 0)
                        
                        # Triangles = indices / 3
                        if mode == 4:  # TRIANGLES
                            total_triangles += index_count // 3
                        elif mode == 5:  # TRIANGLE_STRIP
                            total_triangles += max(0, index_count - 2)
                        elif mode == 6:  # TRIANGLE_FAN
                            total_triangles += max(0, index_count - 2)
                
                # Se não tem índices, usa vértices diretamente
                else:
                    attributes = primitive.get('attributes', {})
                    if 'POSITION' in attributes:
                        position_accessor_idx = attributes['POSITION']
                        if position_accessor_idx < len(accessors):
                            accessor = accessors[position_accessor_idx]
                            vertex_count = accessor.get('count', 0)
                            
                            if mode == 4:  # TRIANGLES
                                total_triangles += vertex_count // 3
                            elif mode in [5, 6]:  # TRIANGLE_STRIP or TRIANGLE_FAN
                                total_triangles += max(0, vertex_count - 2)
        
        return total_triangles
    
    except Exception as e:
        print(f"[gltf_inspector] Erro ao estimar triângulos: {e}", file=sys.stderr)
        return 0


def has_attribute(gltf_json, attribute_name):
    """
    Verifica se algum mesh tem um atributo específico
    
    Args:
        gltf_json: Dict com dados GLTF
        attribute_name: Nome do atributo (ex: 'NORMAL', 'TEXCOORD_0')
        
    Returns:
        Bool indicando se o atributo existe
    """
    try:
        meshes = gltf_json.get('meshes', [])
        
        for mesh in meshes:
            for primitive in mesh.get('primitives', []):
                attributes = primitive.get('attributes', {})
                if attribute_name in attributes:
                    return True
        
        return False
    
    except Exception as e:
        print(f"[gltf_inspector] Erro ao verificar atributo {attribute_name}: {e}", file=sys.stderr)
        return False


def format_file_size(size_bytes):
    """Formata tamanho de arquivo em formato legível"""
    for unit in ['B', 'KB', 'MB', 'GB']:
        if size_bytes < 1024.0:
            return f"{size_bytes:.2f} {unit}"
        size_bytes /= 1024.0
    return f"{size_bytes:.2f} TB"


def print_info(info):
    """Imprime informações de forma legível"""
    if not info:
        return
    
    print("\n=== GLTF File Information ===")
    print(f"File: {info['file_path']}")
    print(f"Size: {format_file_size(info['file_size'])}")
    print(f"Version: {info['version']}")
    
    print("\n--- Structure ---")
    print(f"Nodes: {info['nodes']}")
    print(f"Meshes: {info['meshes']}")
    print(f"Materials: {info['materials']}")
    print(f"Textures: {info['textures']}")
    print(f"Images: {info['images']}")
    
    print("\n--- Geometry ---")
    print(f"Vertices: {info['vertex_count']:,}")
    print(f"Triangles: {info['triangle_count']:,}")
    
    print("\n--- Attributes ---")
    print(f"Normals: {'Yes' if info['has_normals'] else 'No'}")
    print(f"Texture Coords: {'Yes' if info['has_texcoords'] else 'No'}")
    print(f"Tangents: {'Yes' if info['has_tangents'] else 'No'}")
    print(f"Vertex Colors: {'Yes' if info['has_colors'] else 'No'}")
    
    if info['extensions_used']:
        print("\n--- Extensions ---")
        for ext in info['extensions_used']:
            required = " (required)" if ext in info['extensions_required'] else ""
            print(f"  - {ext}{required}")
    
    print("="*40)


def main():
    if len(sys.argv) < 2:
        print("Usage: python gltf_inspector.py <path_to_glb_file>")
        print("       python gltf_inspector.py --json <path_to_glb_file>")
        sys.exit(1)
    
    output_json = False
    glb_path = sys.argv[1]
    
    if sys.argv[1] == "--json" and len(sys.argv) >= 3:
        output_json = True
        glb_path = sys.argv[2]
    
    info = inspect_gltf_file(glb_path)
    
    if info:
        if output_json:
            # Saída em JSON para fácil parsing
            print(json.dumps(info, indent=2))
        else:
            # Saída formatada para leitura humana
            print_info(info)
        sys.exit(0)
    else:
        sys.exit(1)


if __name__ == "__main__":
    main()

