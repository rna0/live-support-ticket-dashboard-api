SELECT id, session_id, sender_id, sender_type, text, attachments, created_at
FROM messages
WHERE id = @id;

