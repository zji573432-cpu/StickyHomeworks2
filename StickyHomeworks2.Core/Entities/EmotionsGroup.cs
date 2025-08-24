using System;
using System.Collections.Generic;

namespace StickyHomeworks.Core.Entities;

public partial class EmotionsGroup
{
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public decimal? Size { get; set; }
}
