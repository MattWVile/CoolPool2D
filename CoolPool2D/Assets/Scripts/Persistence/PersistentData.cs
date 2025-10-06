using System;
using System.Collections.Generic;
using JetBrains.Annotations;

[Serializable]
public class Data {
    public MapData MapData;
}

[Serializable]
public class MapData
{
    public List<VirtualMapNode> GeneratedMap;
    [CanBeNull] public VirtualMapNode CurrentNode;
}
