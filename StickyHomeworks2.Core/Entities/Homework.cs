using System;
using System.Collections.Generic;

namespace StickyHomeworks.Core.Entities;

public partial class Homework
{
    public int Id { get; set; }

    public string? Subject { get; set; }

    public string? Tags { get; set; }

    public string? Content { get; set; }

    public DateTime? EndTime { get; set; }
}
