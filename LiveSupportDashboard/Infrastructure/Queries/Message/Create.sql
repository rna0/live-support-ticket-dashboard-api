INSERT INTO messages (id, session_id, sender_id, sender_type, text, attachments, created_at)
VALUES (@id, @session_id, @sender_id, @sender_type, @text, @attachments::jsonb, @now)

