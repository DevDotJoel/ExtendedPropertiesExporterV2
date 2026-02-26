SELECT  
       O.name AS [Object Name], 
          c.name AS [ColumnName],
          ep.name, 
          ep.value AS [Extended property]
FROM sys.extended_properties EP
INNER JOIN sys.all_objects O ON ep.major_id = O.object_id 
INNER JOIN sys.schemas S on O.schema_id = S.schema_id
INNER JOIN sys.columns AS c ON ep.major_id = c.object_id AND ep.minor_id = c.column_id
where O.name  = '{{TableName}}'
and c.name = '{{ColumnName}}'
