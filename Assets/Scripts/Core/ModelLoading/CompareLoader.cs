using System.Threading.Tasks;
using UnityEngine;

public class CompareLoader : MonoBehaviour
{
    [Header("Refs")]
    public ModelViewer viewer;
    public Transform anchor; // onde os 2 modelos vão ficar (ex.: o mesmo spawnParent)
    public CompareSplitView splitView; // arraste o CompareSplitView no Inspector
    
    [Header("Model/Variants")]
    public string modelA = "Duck", variantA = "original";
    public string modelB = "Duck", variantB = "draco";

    [Header("Layers")]
    public string layerA = "CompareA";
    public string layerB = "CompareB";

    GameObject _a, _b;

    public async Task LoadBothAsync()
    {
        Debug.Log($"[CompareLoader] Starting LoadBothAsync: {modelA}/{variantA} vs {modelB}/{variantB}");
        
        int la = LayerMask.NameToLayer(layerA);
        int lb = LayerMask.NameToLayer(layerB);
        if (la < 0 || lb < 0) 
        { 
            Debug.LogError($"[CompareLoader] Camadas não encontradas: {layerA}({la}) / {layerB}({lb})"); 
            return; 
        }

        Clear();

        Debug.Log($"[CompareLoader] Loading model A: {modelA}/{variantA} into layer {layerA}({la})");
        try 
        {
            _a = await viewer.LoadIntoAsync(modelA, variantA, anchor, la);
            Debug.Log($"[CompareLoader] Model A result: {(_a != null ? "SUCCESS" : "FAILED")}");
            if (_a != null)
            {
                Debug.Log($"[CompareLoader] Model A layer check: root={_a.layer}, expected={la}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CompareLoader] Exception loading model A: {ex.Message}");
            _a = null;
        }
        
        Debug.Log($"[CompareLoader] Loading model B: {modelB}/{variantB} into layer {layerB}({lb})");
        try 
        {
            _b = await viewer.LoadIntoAsync(modelB, variantB, anchor, lb);
            Debug.Log($"[CompareLoader] Model B result: {(_b != null ? "SUCCESS" : "FAILED")}");
            if (_b != null)
            {
                Debug.Log($"[CompareLoader] Model B layer check: root={_b.layer}, expected={lb}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CompareLoader] Exception loading model B: {ex.Message}");
            _b = null;
        }

        if (_a == null || _b == null)
        {
            Debug.LogError($"[CompareLoader] Falha ao carregar: A={_a != null}, B={_b != null}");
            Clear();
        }
        else
        {
            Debug.Log("[CompareLoader] Both models loaded successfully, setting up positioning");
            // opcional: alinhar posição e rotação (preserva escala aplicada pelo NormalizeModelScale)
            _a.transform.localPosition = Vector3.zero;
            _b.transform.localPosition = Vector3.zero;
            _a.transform.localRotation = Quaternion.identity;
            _b.transform.localRotation = Quaternion.identity;
            // NÃO resetar escala aqui - preserva escala automática aplicada no LoadIntoAsync

            // atualizar labels do splitView
            if (splitView)
            {
                splitView.SetSideInfo($"{modelA} ({variantA})", $"{modelB} ({variantB})");
                Debug.Log("[CompareLoader] Updated split view labels");
            }
            
            // Debug: verifica se as câmeras estão vendo apenas suas respectivas layers
            DebugCameraLayers();
        }

        // log
        Debug.Log($"[Compare] A: {modelA}/{variantA} -> {(_a ? "OK" : "FAIL")} | " +
                  $"B: {modelB}/{variantB} -> {(_b ? "OK" : "FAIL")}");
    }

    public void Clear()
    {
        if (_a) Destroy(_a);
        if (_b) Destroy(_b);
        _a = _b = null;
    }

    // Helper para debug: verifica configuração das câmeras
    public void DebugCameraLayers()
    {
        if (splitView && splitView.camA && splitView.camB)
        {
            Debug.Log($"[CompareLoader] Camera A culling mask: {splitView.camA.cullingMask} (binary: {System.Convert.ToString(splitView.camA.cullingMask, 2)})");
            Debug.Log($"[CompareLoader] Camera B culling mask: {splitView.camB.cullingMask} (binary: {System.Convert.ToString(splitView.camB.cullingMask, 2)})");
            
            int la = LayerMask.NameToLayer(layerA);
            int lb = LayerMask.NameToLayer(layerB);
            
            bool camASeesLayerA = (splitView.camA.cullingMask & (1 << la)) != 0;
            bool camASeesLayerB = (splitView.camA.cullingMask & (1 << lb)) != 0;
            bool camBSeesLayerA = (splitView.camB.cullingMask & (1 << la)) != 0;
            bool camBSeesLayerB = (splitView.camB.cullingMask & (1 << lb)) != 0;
            
            Debug.Log($"[CompareLoader] Camera A vê layer {layerA}({la}): {camASeesLayerA}");
            Debug.Log($"[CompareLoader] Camera A vê layer {layerB}({lb}): {camASeesLayerB} ← deveria ser FALSE");
            Debug.Log($"[CompareLoader] Camera B vê layer {layerA}({la}): {camBSeesLayerA} ← deveria ser FALSE");
            Debug.Log($"[CompareLoader] Camera B vê layer {layerB}({lb}): {camBSeesLayerB}");
        }
    }
}
