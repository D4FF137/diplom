# Скрипт для ручного применения миграций через SQL для ВСЕХ сервисов

Write-Host "--- Manual Migration Application Script (All Services) ---"
Write-Host ""

# CompanyService
Write-Host "Applying CompanyService migration..."
docker exec postgres psql -U postgres -d companyservice_db -c @"
CREATE TABLE IF NOT EXISTS companies (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    createdat TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
    MigrationId VARCHAR(150) PRIMARY KEY,
    ProductVersion VARCHAR(32) NOT NULL
);

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20240101000000_InitialCreate', '8.0.0')
ON CONFLICT (MigrationId) DO NOTHING;
"@
Write-Host "CompanyService migration applied."
Write-Host ""

# UserService
Write-Host "Applying UserService migration..."
docker exec postgres psql -U postgres -d userservice_db -c @"
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    companyid INTEGER NOT NULL,
    email VARCHAR(255) NOT NULL,
    passwordhash VARCHAR(255) NOT NULL,
    firstname VARCHAR(100) NOT NULL,
    lastname VARCHAR(100) NOT NULL,
    avatarurl TEXT,
    createdat TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_users_companyid ON users(companyid);
CREATE UNIQUE INDEX IF NOT EXISTS IX_users_email ON users(email);

ALTER TABLE users ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS company_isolation_policy ON users;
CREATE POLICY company_isolation_policy ON users
    FOR ALL
    USING (companyid = current_setting('app.current_company_id', true)::int);

CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
    MigrationId VARCHAR(150) PRIMARY KEY,
    ProductVersion VARCHAR(32) NOT NULL
);

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20240101000000_InitialCreate', '8.0.0')
ON CONFLICT (MigrationId) DO NOTHING;
"@
Write-Host "UserService migration applied."
Write-Host ""

# ChatService
Write-Host "Applying ChatService migration..."
docker exec postgres psql -U postgres -d chatservice_db -c @"
CREATE TABLE IF NOT EXISTS chats (
    id SERIAL PRIMARY KEY,
    companyid INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL,
    createdat TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS messages (
    id SERIAL PRIMARY KEY,
    companyid INTEGER NOT NULL,
    chatid INTEGER NOT NULL,
    userid INTEGER NOT NULL,
    content TEXT NOT NULL,
    createdat TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_messages_chats_chatid FOREIGN KEY (chatid) REFERENCES chats(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS chatmembers (
    id SERIAL PRIMARY KEY,
    companyid INTEGER NOT NULL,
    chatid INTEGER NOT NULL,
    userid INTEGER NOT NULL,
    joinedat TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_chatmembers_chats_chatid FOREIGN KEY (chatid) REFERENCES chats(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_chats_companyid ON chats(companyid);
CREATE INDEX IF NOT EXISTS IX_messages_chatid ON messages(chatid);
CREATE INDEX IF NOT EXISTS IX_messages_companyid ON messages(companyid);
CREATE INDEX IF NOT EXISTS IX_messages_userid ON messages(userid);
CREATE INDEX IF NOT EXISTS IX_chatmembers_companyid ON chatmembers(companyid);
CREATE INDEX IF NOT EXISTS IX_chatmembers_chatid ON chatmembers(chatid);
CREATE INDEX IF NOT EXISTS IX_chatmembers_userid ON chatmembers(userid);
CREATE UNIQUE INDEX IF NOT EXISTS IX_chatmembers_chatid_userid ON chatmembers(chatid, userid);

ALTER TABLE chats ENABLE ROW LEVEL SECURITY;
ALTER TABLE messages ENABLE ROW LEVEL SECURITY;
ALTER TABLE chatmembers ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS company_isolation_policy ON chats;
CREATE POLICY company_isolation_policy ON chats
    FOR ALL
    USING (companyid = current_setting('app.current_company_id', true)::int);

DROP POLICY IF EXISTS company_isolation_policy ON messages;
CREATE POLICY company_isolation_policy ON messages
    FOR ALL
    USING (companyid = current_setting('app.current_company_id', true)::int);

DROP POLICY IF EXISTS company_isolation_policy ON chatmembers;
CREATE POLICY company_isolation_policy ON chatmembers
    FOR ALL
    USING (companyid = current_setting('app.current_company_id', true)::int);

CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
    MigrationId VARCHAR(150) PRIMARY KEY,
    ProductVersion VARCHAR(32) NOT NULL
);

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20240101000000_InitialCreate', '8.0.0')
ON CONFLICT (MigrationId) DO NOTHING;
"@
Write-Host "ChatService migration applied."
Write-Host ""

# FeedService
Write-Host "Applying FeedService migration..."
docker exec postgres psql -U postgres -d feedservice_db -c @"
CREATE TABLE IF NOT EXISTS posts (
    id SERIAL PRIMARY KEY,
    companyid INTEGER NOT NULL,
    userid INTEGER NOT NULL,
    content TEXT NOT NULL,
    imageurl TEXT,
    createdat TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS likes (
    id SERIAL PRIMARY KEY,
    companyid INTEGER NOT NULL,
    postid INTEGER NOT NULL,
    userid INTEGER NOT NULL,
    createdat TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS comments (
    id SERIAL PRIMARY KEY,
    companyid INTEGER NOT NULL,
    postid INTEGER NOT NULL,
    userid INTEGER NOT NULL,
    content TEXT NOT NULL,
    createdat TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS IX_posts_companyid ON posts(companyid);
CREATE INDEX IF NOT EXISTS IX_posts_userid ON posts(userid);
CREATE INDEX IF NOT EXISTS IX_likes_companyid ON likes(companyid);
CREATE INDEX IF NOT EXISTS IX_likes_postid ON likes(postid);
CREATE INDEX IF NOT EXISTS IX_likes_userid ON likes(userid);
CREATE UNIQUE INDEX IF NOT EXISTS IX_likes_postid_userid ON likes(postid, userid);
CREATE INDEX IF NOT EXISTS IX_comments_companyid ON comments(companyid);
CREATE INDEX IF NOT EXISTS IX_comments_postid ON comments(postid);
CREATE INDEX IF NOT EXISTS IX_comments_userid ON comments(userid);

ALTER TABLE posts ENABLE ROW LEVEL SECURITY;
ALTER TABLE likes ENABLE ROW LEVEL SECURITY;
ALTER TABLE comments ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS company_isolation_policy ON posts;
CREATE POLICY company_isolation_policy ON posts
    FOR ALL
    USING (companyid = current_setting('app.current_company_id', true)::int);

DROP POLICY IF EXISTS company_isolation_policy ON likes;
CREATE POLICY company_isolation_policy ON likes
    FOR ALL
    USING (companyid = current_setting('app.current_company_id', true)::int);

DROP POLICY IF EXISTS company_isolation_policy ON comments;
CREATE POLICY company_isolation_policy ON comments
    FOR ALL
    USING (companyid = current_setting('app.current_company_id', true)::int);

CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
    MigrationId VARCHAR(150) PRIMARY KEY,
    ProductVersion VARCHAR(32) NOT NULL
);

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20240101000000_InitialCreate', '8.0.0')
ON CONFLICT (MigrationId) DO NOTHING;
"@
Write-Host "FeedService migration applied."
Write-Host ""

# NotificationService
Write-Host "Applying NotificationService migration..."
docker exec postgres psql -U postgres -d notificationservice_db -c @"
CREATE TABLE IF NOT EXISTS unreadmessages (
    id SERIAL PRIMARY KEY,
    companyid INTEGER NOT NULL,
    chatid INTEGER NOT NULL,
    userid INTEGER NOT NULL,
    count INTEGER NOT NULL,
    lastupdatedat TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE IF NOT EXISTS unreadfeeds (
    id SERIAL PRIMARY KEY,
    companyid INTEGER NOT NULL,
    userid INTEGER NOT NULL,
    count INTEGER NOT NULL,
    lastreadat TIMESTAMP WITH TIME ZONE NOT NULL,
    lastupdatedat TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_unreadmessages_companyid ON unreadmessages(companyid);
CREATE INDEX IF NOT EXISTS IX_unreadmessages_chatid ON unreadmessages(chatid);
CREATE INDEX IF NOT EXISTS IX_unreadmessages_userid ON unreadmessages(userid);
CREATE UNIQUE INDEX IF NOT EXISTS IX_unreadmessages_chatid_userid_companyid ON unreadmessages(chatid, userid, companyid);

CREATE INDEX IF NOT EXISTS IX_unreadfeeds_companyid ON unreadfeeds(companyid);
CREATE INDEX IF NOT EXISTS IX_unreadfeeds_userid ON unreadfeeds(userid);
CREATE UNIQUE INDEX IF NOT EXISTS IX_unreadfeeds_userid_companyid ON unreadfeeds(userid, companyid);

CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
    MigrationId VARCHAR(150) PRIMARY KEY,
    ProductVersion VARCHAR(32) NOT NULL
);

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20240101000000_InitialCreate', '8.0.0')
ON CONFLICT (MigrationId) DO NOTHING;
"@
Write-Host "NotificationService migration applied."
Write-Host ""

Write-Host "--- Migration Application Complete ---"
Write-Host "All services migrations have been applied."
Write-Host ""
Write-Host "Now restart all services:"
Write-Host "  docker-compose restart companyservice userservice chatservice feedservice notificationservice"
Write-Host ""
Write-Host "Or restart all services:"
Write-Host "  docker-compose restart"
