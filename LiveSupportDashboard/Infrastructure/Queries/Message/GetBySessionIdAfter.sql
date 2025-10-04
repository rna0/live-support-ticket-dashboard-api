SELECT id, session_id, sender_id, sender_type, text, attachments, created_at
FROM messages
WHERE session_id = @session_id
  AND created_at > (SELECT created_at FROM messages WHERE id = @after_message_id)
ORDER BY created_at ASC
LIMIT @limit;

