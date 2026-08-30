/*
 * KnotLink SDK - C#
 * Copyright (c) 2024-2026 KnotLink Contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;

namespace KnotLink
{
    public class KLUDF
    {
    }

    public class KLKVMap : Dictionary<string, string>
    {
    public string Serialize()
    {
        // 将 KLKVMap 序列化为键值对字符串
        var pairs = new List<string>();
        foreach (KeyValuePair<string, string> kvp in this)
        {
            pairs.Add($"{kvp.Key}={kvp.Value}");
        }
        return string.Join(";", pairs);
    }

    public void Deserialize(string keyValueString)
    {
        // 将键值对字符串反序列化为 KLKVMap
        this.Clear(); // 清空当前映射
        string[] pairs = keyValueString.Split(';');
        foreach (string pair in pairs)
        {
            string[] keyValue = pair.Split(new[] { '=' }, 2);
            if (keyValue.Length == 2)
            {
                this[keyValue[0]] = keyValue[1];
            }
        }
    }

    public string Get(string key, string defaultVal = "")
    {
        // 安全地读取键值对
        return this.TryGetValue(key, out string? value) ? value ?? defaultVal : defaultVal;
    }
}
}
