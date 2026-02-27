SELECT  
    O.name AS [ObjectName], 
    c.name AS [ColumnName],
    ep.name AS [PropertyName], 
    ep.value AS [PropertyValue]
FROM sys.extended_properties EP
INNER JOIN sys.all_objects O ON ep.major_id = O.object_id 
INNER JOIN sys.schemas S ON O.schema_id = S.schema_id
INNER JOIN sys.columns AS c ON ep.major_id = c.object_id AND ep.minor_id = c.column_id
WHERE O.type = 'U'
ORDER BY O.name, c.name
