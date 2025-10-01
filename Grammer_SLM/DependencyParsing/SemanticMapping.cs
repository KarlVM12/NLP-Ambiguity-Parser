using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

public static class SemanticMapper
{
    public static Dictionary<string, List<DependencyNode>> Map(DependencyNode root)
    {
        //Dictionary<string, List<string>> roles = new Dictionary<string, List<string>>
        //{
        //    ["main_verb"] = new List<string>(),
        //    ["subject"] = new List<string>(),
        //    ["objects"] = new List<string>(),
        //    ["entities"] = new List<string>(),
        //    ["recipients"] = new List<string>(),
        //    ["datetimes"] = new List<string>(),
        //    ["locations"] = new List<string>()
        //};

        //TraverseNode(root, roles);

        Dictionary<string, List<DependencyNode>> roles = new Dictionary<string, List<DependencyNode>>
        {
            ["main_verb"] = new List<DependencyNode>(),
            ["subject"] = new List<DependencyNode>(),
            ["objects"] = new List<DependencyNode>(),
            ["entities"] = new List<DependencyNode>(),
            ["recipients"] = new List<DependencyNode>(),
            ["datetimes"] = new List<DependencyNode>(),
            ["locations"] = new List<DependencyNode>()
        };

        TraverseNode_DependencyNode(root, roles);

        return roles;
    }

    // recursively traverse the tree to map semantics of dependency tree
    private static void TraverseNode(DependencyNode node, Dictionary<string, List<string>> roles)
    {
        // base
        if (node == null) return;

        if (roles["main_verb"].Count == 0 && node.DependencyType == "ROOT")
        {
            roles["main_verb"].Add(node.Word);
        }

        foreach (var child in node.Children)
        {
            // the classics - bad hardcode ! >:( - but they should be classified :)
            bool entityCheck = child.IsEntity || child.Word.ToLower() == "me" || child.Word.ToLower() == "myself" || child.Word.ToLower() == "i";

            // could all be a switch i guess 
            if (child.DependencyType == "nsubj")
            {

                roles["subject"].Add(child.Word);

            }
            else if ((child.DependencyType == "dobj" || child.DependencyType == "pobj") && !child.IsDateTimeUnit && !entityCheck)
            {

                roles["objects"].Add(child.Word);

            }
            else if (((child.DependencyType == "dobj" || child.DependencyType == "pobj") && entityCheck) || (child.DependencyType == "prep" && child.Children.First().IsEntity))
            {
                // doesn't have to be object of phrase, i.e. Tell Ron
                // needs to be more nuanced
                if (child.DependencyType == "prep")
                {
                    roles["recipients"].Add(child.Children.First().Word);
                }
                else
                {
                    roles["recipients"].Add(child.Word);
                }

            }
            else if ((child.DependencyType == "pobj" || child.DependencyType == "dobj") && child.IsDateTimeUnit)
            {

                roles["datetimes"].Add(child.Word);

            }

            if (entityCheck)
            {
                
                roles["entities"].Add(child.Word);
            }

            TraverseNode(child, roles);
        }
    }

    // recursively traverse the tree to map semantics of dependency tree
    private static void TraverseNode_DependencyNode(DependencyNode node, Dictionary<string, List<DependencyNode>> roles)
    {
        // base
        if (node == null) return;

        if (roles["main_verb"].Count == 0 && node.DependencyType == "ROOT")
        {
            roles["main_verb"].Add(node);
        }

        foreach (var child in node.Children)
        {
            // the classics - bad hardcode ! >:( - but they should be classified :)
            bool entityCheck = child.IsEntity || child.Word.ToLower() == "me" || child.Word.ToLower() == "myself" || child.Word.ToLower() == "i";

            // could all be a switch i guess 
            if (child.DependencyType == "nsubj")
            {

                roles["subject"].Add(child);

            }
            else if ((child.DependencyType.Contains("obj")) && !child.IsDateTimeUnit && !entityCheck)
            {

                roles["objects"].Add(child);

            }
            else if (((child.DependencyType.Contains("obj")) && entityCheck) || (child.DependencyType == "prep" && child.Children.First().IsEntity))
            {
                // doesn't have to be object of phrase, i.e. Tell Ron
                // needs to be more nuanced
                if (child.DependencyType == "prep")
                {
                    if (!roles["recipients"].Contains(child.Children.First()))
                    {
                        roles["recipients"].Add(child.Children.First());
                    }
                }
                else
                {
                    if (!roles["recipients"].Contains(child))
                    {
                        roles["recipients"].Add(child);
                    }
                }

            }
            else if ((child.DependencyType == "pobj" || child.DependencyType == "dobj") && child.IsDateTimeUnit)
            {

                roles["datetimes"].Add(child);

            }

            if (entityCheck)
            {
                roles["entities"].Add(child);
            }

            TraverseNode_DependencyNode(child, roles);
        }
    }
}

