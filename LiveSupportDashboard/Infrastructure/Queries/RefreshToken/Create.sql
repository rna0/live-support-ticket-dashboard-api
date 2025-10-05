INSERT INTO refresh_tokens (agent_id, token, expires_at)
VALUES ($1, $2, $3)
  RETURNING id
