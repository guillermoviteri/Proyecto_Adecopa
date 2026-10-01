using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BotonesAlpa
{
    const string carpeta = "Assets/AlpaBalance/Modelos";
    const float celda = 0.1f;
    const int anchoCeldas = 29;
    const int margenX = 4;
    const int margenY = 3;
    const float profPlaca = 0.25f;
    const float profLetra = 0.1f;
    const float radio = 0.25f;
    const int seg = 6;

    static readonly Dictionary<char, string[]> letras = new Dictionary<char, string[]>
    {
        { 'S', new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." } },
        { 'T', new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." } },
        { 'A', new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
        { 'R', new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" } },
        { 'E', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" } },
        { 'X', new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" } },
        { 'I', new[] { "###", ".#.", ".#.", ".#.", ".#.", ".#.", "###" } }
    };

    class Armador
    {
        public List<Vector3> v = new List<Vector3>();
        public List<Vector3> n = new List<Vector3>();
        public List<int> placa = new List<int>();
        public List<int> letra = new List<int>();

        public int Vert(Vector3 p, Vector3 nor)
        {
            v.Add(p);
            n.Add(nor);
            return v.Count - 1;
        }

        public void Tri(List<int> l, int a, int b, int c, Vector3 nor)
        {
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), nor) < 0)
            {
                int t = b;
                b = c;
                c = t;
            }
            l.Add(a);
            l.Add(b);
            l.Add(c);
        }

        public void Quad(List<int> l, int a, int b, int c, int d, Vector3 nor)
        {
            Tri(l, a, b, c, nor);
            Tri(l, a, c, d, nor);
        }
    }

    [MenuItem("AlpaBalance/4. Crear botones 3D Start y Exit", false, 4)]
    static void Crear()
    {
        Directory.CreateDirectory(carpeta);
        var cam = Camera.main;
        if (cam == null)
        {
            EditorUtility.DisplayDialog("AlpaBalance", "No encuentro la cámara principal (tag MainCamera) en la escena abierta.", "OK");
            return;
        }
        MenuAlpa menu = null;
        foreach (var r in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            menu = r.GetComponentInChildren<MenuAlpa>(true);
            if (menu != null) break;
        }

        var blanco = Mat("Letras", Color.white);
        var verde = Mat("PlacaStart", new Color(0.18f, 0.72f, 0.36f));
        var rojo = Mat("PlacaExit", new Color(0.86f, 0.22f, 0.27f));

        float dist = 6f;
        if (menu != null && menu.tierra != null) dist = Mathf.Max(1f, Vector3.Distance(cam.transform.position, menu.tierra.position) * 0.6f);
        float alto = cam.orthographic ? cam.orthographicSize * 2f : 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float escala = alto * 0.085f / ((7 + 2 * margenY) * celda);
        Vector3 centro = cam.transform.position + cam.transform.forward * dist;
        var rot = cam.transform.rotation;

        var start = CrearBoton("BotonStart3D", "START", verde, blanco, centro - cam.transform.up * alto * 0.17f, rot, escala);
        var exit = CrearBoton("BotonExit3D", "EXIT", rojo, blanco, centro - cam.transform.up * alto * 0.3f, rot, escala);

        string aviso = "Creé BotonStart3D y BotonExit3D frente a la cámara.";
        if (menu != null)
        {
            Undo.RecordObject(menu, "Asignar botones 3D");
            Retirar(menu, menu.comenzar3D, start);
            Retirar(menu, menu.salir3D, exit);
            if (menu.botonComenzar != null)
            {
                Undo.RecordObject(menu.botonComenzar.gameObject, "Ocultar botón UI");
                menu.botonComenzar.gameObject.SetActive(false);
            }
            if (menu.botonSalir != null)
            {
                Undo.RecordObject(menu.botonSalir.gameObject, "Ocultar botón UI");
                menu.botonSalir.gameObject.SetActive(false);
            }
            menu.comenzar3D = start;
            menu.salir3D = exit;
            aviso += "\n\nYa quedaron asignados en MenuAlpa y desactivé los botones anteriores (puedes reactivarlos si quieres).";
        }
        else aviso += "\n\nArrástralos a 'Comenzar 3D' y 'Salir 3D' en el componente MenuAlpa.";
        aviso += "\n\nPuedes moverlos y escalarlos como quieras. Los colores están en Assets/AlpaBalance/Modelos.";

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.objects = new Object[] { start.gameObject, exit.gameObject };
        EditorUtility.DisplayDialog("AlpaBalance", aviso, "OK");
    }

    static void Retirar(MenuAlpa menu, Boton3D viejo, Boton3D nuevo)
    {
        if (viejo == null || viejo == nuevo) return;
        var t = menu.tierra;
        bool esTierra = t != null && (viejo.transform == t || viejo.transform.IsChildOf(t) || t.IsChildOf(viejo.transform));
        if (esTierra) Undo.DestroyObjectImmediate(viejo);
        else
        {
            Undo.RecordObject(viejo.gameObject, "Ocultar botón viejo");
            viejo.gameObject.SetActive(false);
        }
    }

    static Boton3D CrearBoton(string nombre, string texto, Material placa, Material letra, Vector3 pos, Quaternion rot, float escala)
    {
        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = Vector3.one * escala;
        var malla = GuardarMalla(texto);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        go.AddComponent<MeshRenderer>().sharedMaterials = new[] { placa, letra };
        var bc = go.AddComponent<BoxCollider>();
        bc.center = malla.bounds.center;
        bc.size = malla.bounds.size;
        return go.AddComponent<Boton3D>();
    }

    static Mesh GuardarMalla(string texto)
    {
        string ruta = carpeta + "/Malla" + texto + ".asset";
        var nueva = CrearMalla(texto);
        var vieja = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        if (vieja == null)
        {
            AssetDatabase.CreateAsset(nueva, ruta);
            return nueva;
        }
        vieja.Clear();
        EditorUtility.CopySerialized(nueva, vieja);
        Object.DestroyImmediate(nueva);
        EditorUtility.SetDirty(vieja);
        return vieja;
    }

    static Material Mat(string nombre, Color color)
    {
        string ruta = carpeta + "/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            Shader sh = null;
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("HDRP/Lit");
            }
            if (sh == null) sh = Shader.Find("Standard");
            mat = new Material(sh);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.55f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.55f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static Mesh CrearMalla(string texto)
    {
        var m = new Armador();
        float w = (anchoCeldas + 2 * margenX) * celda;
        float h = (7 + 2 * margenY) * celda;
        float zf = -profPlaca / 2f;
        float zb = profPlaca / 2f;

        var contorno = new List<Vector4>();
        var esquinas = new[]
        {
            new Vector3(w / 2 - radio, h / 2 - radio, 0),
            new Vector3(-w / 2 + radio, h / 2 - radio, 90),
            new Vector3(-w / 2 + radio, -h / 2 + radio, 180),
            new Vector3(w / 2 - radio, -h / 2 + radio, 270)
        };
        foreach (var e in esquinas)
        {
            for (int k = 0; k <= seg; k++)
            {
                float ang = (e.z + 90f * k / seg) * Mathf.Deg2Rad;
                contorno.Add(new Vector4(e.x + radio * Mathf.Cos(ang), e.y + radio * Mathf.Sin(ang), Mathf.Cos(ang), Mathf.Sin(ang)));
            }
        }
        int total = contorno.Count;
        foreach (float z in new[] { zf, zb })
        {
            var nor = new Vector3(0, 0, z < 0 ? -1 : 1);
            int centro = m.Vert(new Vector3(0, 0, z), nor);
            var ids = new int[total];
            for (int i = 0; i < total; i++) ids[i] = m.Vert(new Vector3(contorno[i].x, contorno[i].y, z), nor);
            for (int i = 0; i < total; i++) m.Tri(m.placa, centro, ids[i], ids[(i + 1) % total], nor);
        }
        for (int i = 0; i < total; i++)
        {
            Vector4 p = contorno[i];
            Vector4 q = contorno[(i + 1) % total];
            var np = new Vector3(p.z, p.w, 0);
            var nq = new Vector3(q.z, q.w, 0);
            int v1 = m.Vert(new Vector3(p.x, p.y, zf), np);
            int v2 = m.Vert(new Vector3(q.x, q.y, zf), nq);
            int v3 = m.Vert(new Vector3(q.x, q.y, zb), nq);
            int v4 = m.Vert(new Vector3(p.x, p.y, zb), np);
            m.Quad(m.placa, v1, v2, v3, v4, np + nq);
        }

        var lleno = new HashSet<Vector2Int>();
        int col = 0;
        for (int i = 0; i < texto.Length; i++)
        {
            if (!letras.TryGetValue(char.ToUpperInvariant(texto[i]), out var g)) continue;
            if (col > 0) col++;
            for (int x = 0; x < g[0].Length; x++, col++)
                for (int y = 0; y < 7; y++)
                    if (g[y][x] == '#') lleno.Add(new Vector2Int(col, y));
        }

        float x0 = -col * celda / 2f;
        float y0 = 7 * celda / 2f;
        float zl = zf - profLetra;
        var fr = new Vector3(0, 0, -1);
        foreach (var cel in lleno)
        {
            float izq = x0 + cel.x * celda;
            float der = izq + celda;
            float arr = y0 - cel.y * celda;
            float aba = arr - celda;
            m.Quad(m.letra, m.Vert(new Vector3(izq, aba, zl), fr), m.Vert(new Vector3(izq, arr, zl), fr), m.Vert(new Vector3(der, arr, zl), fr), m.Vert(new Vector3(der, aba, zl), fr), fr);
            Lado(m, lleno, cel, -1, 0, new Vector2(izq, aba), new Vector2(izq, arr), zl, zf);
            Lado(m, lleno, cel, 1, 0, new Vector2(der, arr), new Vector2(der, aba), zl, zf);
            Lado(m, lleno, cel, 0, -1, new Vector2(der, arr), new Vector2(izq, arr), zl, zf);
            Lado(m, lleno, cel, 0, 1, new Vector2(izq, aba), new Vector2(der, aba), zl, zf);
        }

        var malla = new Mesh { name = "Boton" + texto };
        malla.SetVertices(m.v);
        malla.SetNormals(m.n);
        malla.subMeshCount = 2;
        malla.SetTriangles(m.placa, 0);
        malla.SetTriangles(m.letra, 1);
        malla.RecalculateBounds();
        return malla;
    }

    static void Lado(Armador m, HashSet<Vector2Int> lleno, Vector2Int cel, int dx, int dy, Vector2 p1, Vector2 p2, float zl, float zf)
    {
        if (lleno.Contains(new Vector2Int(cel.x + dx, cel.y + dy))) return;
        var nor = new Vector3(dx, -dy, 0);
        m.Quad(m.letra, m.Vert(new Vector3(p1.x, p1.y, zl), nor), m.Vert(new Vector3(p2.x, p2.y, zl), nor), m.Vert(new Vector3(p2.x, p2.y, zf), nor), m.Vert(new Vector3(p1.x, p1.y, zf), nor), nor);
    }
}
