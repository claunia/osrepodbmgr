SELECT
  id,
  CONCAT(
      '"',
      Product, ' ', Version,

      IF(architecture <> 'ia32'
           AND architecture <> ''
           AND architecture IS NOT NULL,
         CONCAT(' (', architecture, ')'),
         ''
      ),

      IF(languages <> 'eng'
           AND languages <> ''
           AND languages IS NOT NULL,
         CONCAT(' (', languages, ')'),
         ''
      ),

      IF(files = 1, ' (files)', ''),
      IF(`update` = 1, ' (update)', ''),
      IF(upgrade = 1, ' (upgrade)', ''),

      IF(description IS NOT NULL
           AND description <> '',
         CONCAT(' (', description, ')'),
         ''
      ),

      '"'
  ) AS description
FROM oses WHERE source = 0 ORDER BY description;
