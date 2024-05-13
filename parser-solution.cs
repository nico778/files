using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Globalization;

[RequireComponent(typeof(MeshFilter))]
public class ObjFileParser : MonoBehaviour
{
    Mesh mesh;

    void Start()
    {
       mesh = new Mesh();
       GetComponent<MeshFilter>().mesh = mesh;
       ReadFile("/path/to/wherever/your/file/is.obj");
    }

    void ReadFile(string filePath) {
       List<Vector3> vertices = new List<Vector3>();
       List<int> triangles = new List<int>();

       StreamReader reader = new StreamReader(filePath);

       while (!reader.EndOfStream)
       {
           string line = reader.ReadLine().Trim();
           if (line == "") continue; // skip empty lines

           string[] values = line.Split(' ');

           if (values[0] == "v")
           {
               float x = float.Parse(values[1], CultureInfo.InvariantCulture);
               float y = float.Parse(values[2], CultureInfo.InvariantCulture);
               float z = float.Parse(values[3], CultureInfo.InvariantCulture);
               vertices.Add(new Vector3(x, y, z));
           }
           else if (values[0] == "f")
           {
               int v1 = int.Parse(values[1].Split('/')[0]) - 1;
               int v2 = int.Parse(values[2].Split('/')[0]) - 1;
               int v3 = int.Parse(values[3].Split('/')[0]) - 1;

               triangles.Add(v1);
               triangles.Add(v2);
               triangles.Add(v3);
           }
    }

       reader.Close();

       mesh.Clear();
       mesh.vertices = vertices.ToArray();
       mesh.triangles = triangles.ToArray();
       mesh.RecalculateNormals();
   }
}

    

