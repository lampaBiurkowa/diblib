﻿using DibBase.ModelBase;

namespace DibBase.Models;

public class Event : Entity
{
    public DateTime CreatedAt { get; set; }
    public required string Payload { get; set; }
    public required string Name { get; set; }
    public bool IsPublished { get; set; }
}
