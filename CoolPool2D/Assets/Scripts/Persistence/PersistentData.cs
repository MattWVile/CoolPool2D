using System;
using System.Collections.Generic;
using JetBrains.Annotations;

[Serializable]
public class Data {
    public MapData MapData;
    public InventoryData InventoryData;
}

[Serializable]
public class MapData {
    public List<VirtualMapNode> GeneratedMap;
    [CanBeNull] public VirtualMapNode CurrentNode;
}

[Serializable]
public class InventoryData {
    public List<BallColour> OwnedCueBalls;
}
