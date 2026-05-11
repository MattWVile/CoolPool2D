using System;
using System.Collections.Generic;
using JetBrains.Annotations;

[Serializable]
public class Data {
    public MapData MapData;
    public InventoryData InventoryData;
    public ScoreData ScoreData;
}

[Serializable]
public class MapData {
    public List<VirtualMapNode> GeneratedMap;
    [CanBeNull] public VirtualMapNode CurrentNode;
}

[Serializable]
public class InventoryData {
    public List<BallVariant> OwnedCueBalls;
}
[Serializable]
public class ScoreData {
    public int HighScore;
}
