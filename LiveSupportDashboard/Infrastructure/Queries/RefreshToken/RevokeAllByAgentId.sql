UPDATE refresh_tokens
SET is_revoked = TRUE, revoked_at = NOW()
WHERE agent_id = $1 AND is_revoked = FALSE

