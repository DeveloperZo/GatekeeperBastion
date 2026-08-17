using System;
using System.Collections.Generic;
using GatekeepersProtocol.EditorTools;
using GatekeepersProtocol.Game;
using NUnit.Framework;
using UnityEngine;

namespace GatekeepersProtocol.Tests
{
    public class StationPanelDriftTests
    {
        [Test]
        public void SceneGateStations_MatchEveryConcreteStationTerminal()
        {
            var names = new List<string>();
            Type baseType = typeof(StationTerminal);
            foreach (Type type in baseType.Assembly.GetTypes())
            {
                if (type.IsAbstract || !baseType.IsAssignableFrom(type)) continue;
                var go = new GameObject(type.Name);
                try
                {
                    var station = (StationTerminal)go.AddComponent(type);
                    names.Add(station.StationName);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            CollectionAssert.AreEquivalent(StationSceneGate.Stations, names);
            Assert.AreEqual(StationSceneGate.Stations.Length, names.Count);
        }
    }
}
