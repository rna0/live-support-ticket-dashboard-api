SELECT id, name, email, password_hash, created_at, updated_at
FROM agents
{whereClause}
ORDER BY
    -- Exact matches first
    CASE WHEN LOWER(name) = LOWER(@searchTerm) THEN 1 ELSE 0 END DESC,
    CASE WHEN LOWER(email) = LOWER(@searchTerm) THEN 2 ELSE 0 END DESC,
    -- Starts with matches
    CASE WHEN LOWER(name) LIKE LOWER(@searchTerm) || '%' THEN 3 ELSE 0 END DESC,
    CASE WHEN LOWER(email) LIKE LOWER(@searchTerm) || '%' THEN 4 ELSE 0 END DESC,
    -- Then alphabetically by name
    name
LIMIT @limit OFFSET @offset
