//using UnityEngine;
////namespace PieChart.ViitorCloud;
//using System.Collections.Generic;

//public class RuntimeProtocolPieChart : MonoBehaviour
//{
//    public LoadData loadData;
//    public PiechartManager piechartManager;

//    public void ShowPieChart()
//    {
//        if (loadData == null || piechartManager == null)
//        {
//            Debug.LogError("LoadData o PiechartManager non assegnati.");
//            return;
//        }

//        if (!loadData.isLoaded)
//        {
//            Debug.LogWarning("I dati non sono ancora caricati.");
//            return;
//        }

//        Dictionary<string, int> protocolCounts = new Dictionary<string, int>();
//        int total = 0;

//        foreach (var row in loadData.data)
//        {
//            if (row.TryGetValue("Protocol", out string protocol))
//            {
//                if (!protocolCounts.ContainsKey(protocol))
//                    protocolCounts[protocol] = 0;

//                protocolCounts[protocol]++;
//                total++;
//            }
//        }

//        List<string> labels = new List<string>();
//        List<string> descriptions = new List<string>();
//        List<float> values = new List<float>();
//        List<Color> colors = new List<Color>();

//        foreach (var entry in protocolCounts)
//        {
//            float percent = (float)entry.Value / total * 100f;
//            labels.Add(entry.Key);
//            descriptions.Add($"{entry.Value} flows\n{percent:F1}%");
//            values.Add(entry.Value);
//            colors.Add(Random.ColorHSV());
//        }

//        var controller = piechartManager.pieChartMeshController;
//        controller.randomData = false;
//        controller.randomColor = false;
//        controller.segments = values.Count;
//        controller.Data = values.ToArray();
//        controller.dataHeadername = labels;
//        controller.dataDescription = descriptions;
//        controller.customColors = colors.ToArray();
//        controller.animationType = PieChartMeshController.AnimationType.UpDownAndRotation;

//        controller.GenerateChart();
//    }
//}
