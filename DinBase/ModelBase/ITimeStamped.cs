﻿namespace DibBase.ModelBase;

public interface ITimeStamped
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
