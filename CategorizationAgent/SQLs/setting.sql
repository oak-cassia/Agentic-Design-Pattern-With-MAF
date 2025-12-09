CREATE DATABASE game_log_db;
USE game_log_db;

CREATE TABLE mailbox_logs (
                              id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT '로그 고유 ID',
                              user_id BIGINT NOT NULL COMMENT '유저 ID',
                              message_id VARCHAR(50) NOT NULL COMMENT '메시지 고유 ID (우편 ID)',
                              item_type INT NOT NULL COMMENT '아이템 타입 (예: 1=Gold, 2=Gem)',
                              item_id INT NOT NULL COMMENT '아이템 ID',
                              mail_state TINYINT NOT NULL COMMENT '상태 (0: 수령전, 1: 수령완료, 2: 만료/삭제)',
                              created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '로그 생성 시간',

                              CHECK (mail_state IN (0, 1, 2)),

                              INDEX idx_user_message (user_id, message_id)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

CREATE TABLE user_numbers (
                              id INT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '실제 번호 (auto increment, auto-increment primary key)',
                              user_id VARCHAR(20) NOT NULL COMMENT '유저 ID (user id)',

                              PRIMARY KEY (id),                          -- 기본 키 (primary key)
                              UNIQUE KEY uk_user_numbers_user_id (user_id)  -- 유니크 키 (unique key)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

INSERT INTO user_numbers (user_id) VALUES
                                       ('user_1001'),
                                       ('user_1002'),
                                       ('user_1003'),
                                       ('user_1004'),
                                       ('user_1005'),
                                       ('user_2001'),
                                       ('user_2002'),
                                       ('user_2003'),
                                       ('user_2004'),
                                       ('user_2005'),
                                       ('user_3001'),
                                       ('user_3002'),
                                       ('user_3003'),
                                       ('user_3004'),
                                       ('user_3005'),
                                       ('user_4001'),
                                       ('user_4002'),
                                       ('user_4003'),
                                       ('user_4004'),
                                       ('user_4005'),
                                       ('user_5001'),
                                       ('user_5002'),
                                       ('user_5003'),
                                       ('user_5004'),
                                       ('user_5005'),
                                       ('user_6001'),
                                       ('user_6002'),
                                       ('user_6003'),
                                       ('user_6004'),
                                       ('user_6005'),
                                       ('user_7001'),
                                       ('user_7002'),
                                       ('user_7003'),
                                       ('user_7004'),
                                       ('user_7005'),
                                       ('user_8001'),
                                       ('user_8002'),
                                       ('user_8003'),
                                       ('user_8004'),
                                       ('user_8005'),
                                       ('user_9801'),
                                       ('user_9802'),
                                       ('user_9803'),
                                       ('user_9804'),
                                       ('user_9805'),
                                       ('user_9901'),
                                       ('user_9902'),
                                       ('user_9903'),
                                       ('user_9904'),
                                       ('user_9905');

INSERT INTO mailbox_logs (user_id, message_id, item_type, item_id, mail_state)
VALUES
    (1, 1, 2, 101, 0), -- user_1001
    (2, 1, 2, 101, 1), -- user_1002
    (3, 1, 2, 101, 2); -- user_1003


