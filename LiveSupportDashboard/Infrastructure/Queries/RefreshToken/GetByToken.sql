SELECT id, agent_id, token, expires_at, created_at, revoked_at, is_revoked
FROM refresh_tokens
WHERE token = $1

