SELECT COUNT(*)
FROM refresh_tokens
WHERE token = $1
  AND is_revoked = FALSE
  AND expires_at > NOW()


