SELECT COLUMN_NAME AS 'ColumnName'
            , TABLE_NAME AS  'TableName'
FROM INFORMATION_SCHEMA.COLUMNS
WHERE       TABLE_NAME LIKE '{{TableName}}'
ORDER BY    TableName
            ,ColumnName;
