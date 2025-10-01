using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

public class DependencyNode
{
    public string Hash { get; set; }
    public string Word { get; set; }
    public string PartOfSpeech { get; set; }
    public int Index { get; set; }
    public List<DependencyNode> Children { get; set; }
    public DependencyNode Head { get; set; }
    public string DependencyType { get; set; }

    public bool IsPossessive { get; set; }
    public bool IsAuxiliary { get; set; }
    public bool IsDateTimeUnit { get; set; }
    public bool IsEntity { get; set; }
    public bool IsNumeral { get; set; }

    public DependencyNode()
    {
        Children = new List<DependencyNode>();
    }
}
