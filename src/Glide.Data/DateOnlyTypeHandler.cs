using System;
using System.Data;

using Dapper;

namespace Glide.Data;

public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value)
    {
        if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
        {
            return DateOnly.Parse(stringValue);
        }

        throw new ArgumentException($"Cannot convert {value} to DateOnly");
    }

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.Value = value.ToString("yyyy-MM-dd");
        parameter.DbType = DbType.String;
    }
}

public class NullableDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly?>
{
    public override DateOnly? Parse(object value)
    {
        if (value == null || value is DBNull)
        {
            return null;
        }

        if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
        {
            return DateOnly.Parse(stringValue);
        }

        return null;
    }

    public override void SetValue(IDbDataParameter parameter, DateOnly? value)
    {
        if (value.HasValue)
        {
            parameter.Value = value.Value.ToString("yyyy-MM-dd");
            parameter.DbType = DbType.String;
        }
        else
        {
            parameter.Value = DBNull.Value;
            parameter.DbType = DbType.String;
        }
    }
}
