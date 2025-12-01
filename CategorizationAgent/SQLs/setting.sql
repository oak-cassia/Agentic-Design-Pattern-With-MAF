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

