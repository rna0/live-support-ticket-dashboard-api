﻿SELECT id, name, email, password_hash, created_at, updated_at
FROM agents
WHERE id = @id
