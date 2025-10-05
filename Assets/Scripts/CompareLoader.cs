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
            // opcional: alinhar rotação/escala se necessário
            _a.transform.localPosition = Vector3.zero;
            _b.transform.localPosition = Vector3.zero;
            _a.transform.localRotation = Quaternion.identity;
            _b.transform.localRotation = Quaternion.identity;
            _a.transform.localScale    = Vector3.one;
            _b.transform.localScale    = Vector3.one;

            // atualizar labels do splitView
            if (splitView)
            {
                splitView.SetSideInfo($"{modelA} ({variantA})", $"{modelB} ({variantB})");
                Debug.Log("[CompareLoader] Updated split view labels");
            }
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

    // Wrapper público para ser chamado por botões na UI
    public void LoadBoth()
    {
        _ = LoadBothAsync();
    }
}
