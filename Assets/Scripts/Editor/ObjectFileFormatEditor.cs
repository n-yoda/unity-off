using UnityEngine;
using UnityEditor;
using System.IO;

public class ObjectFileFormatEditor
{
    [MenuItem("Assets/OFF/Create Mesh From OFF")]
    public static void CreateMeshFromOff()
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        var mesh = ObjectFileFormat.OffToMesh(new StreamReader(path));
        var newPath = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(path, ".asset"));
        AssetDatabase.CreateAsset(mesh, newPath);
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/OFF/Create OFF From Mesh")]
    public static void CreateOffFromMesh()
    {
        var mesh = Selection.activeObject as Mesh;
        if (mesh == null) {
            throw new System.ArgumentException("No mesh selected.");
        }
        var path = AssetDatabase.GetAssetPath(mesh);
        var newPath = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(path, ".off"));
        ObjectFileFormat.MeshToOff(mesh, new StreamWriter(newPath));
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/OFF/Recalculate Mesh Normals")]
    public static void RecalculateNormals()
    {
        var mesh = Selection.activeObject as Mesh;
        if (mesh == null) {
            throw new System.ArgumentException("No mesh selected.");
        }
        mesh.RecalculateNormals();
        EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();
    }
}
