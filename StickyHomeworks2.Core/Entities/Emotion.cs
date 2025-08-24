using System;
using System.Collections.Generic;

namespace StickyHomeworks.Core.Entities;

public partial class Emotion
{
    public string Id { get; set; } = null!;

    public string? Path { get; set; }

    public string? GroupId { get; set; }

    public int? Size { get; set; }
}
