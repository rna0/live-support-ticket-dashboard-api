UPDATE refresh_tokens
SET is_revoked = TRUE, revoked_at = NOW()
WHERE token = $1

