-- ============================================
-- Oracle Database Setup Script for CleanArchitectureDemo
-- ============================================

-- ============================================
-- STEP 1: Create Tables
-- ============================================

-- Create Users Table
CREATE TABLE Users (
    Id VARCHAR2(36) PRIMARY KEY,
    Email VARCHAR2(255) NOT NULL UNIQUE
);

-- Create Index on Email for better query performance
CREATE INDEX IX_Users_Email ON Users(Email);

-- ============================================
-- STEP 2: Create Packages (Optional - for grouping)
-- ============================================

-- Package specification for User operations
CREATE OR REPLACE PACKAGE PKG_USER_OPERATIONS AS
    -- Type for cursor return
    TYPE T_USER_CURSOR IS REF CURSOR;
END PKG_USER_OPERATIONS;
/

-- ============================================
-- STEP 3: Query Functions (READ Operations)
-- ============================================

-- Function: Get User by ID
CREATE OR REPLACE FUNCTION FN_GET_USER_BY_ID(
    p_id IN VARCHAR2
)
RETURN SYS_REFCURSOR
IS
    v_cursor SYS_REFCURSOR;
BEGIN
    OPEN v_cursor FOR
        SELECT Id, Email 
        FROM Users 
        WHERE Id = p_id;

    RETURN v_cursor;
END FN_GET_USER_BY_ID;
/

-- Function: Get User by Email
CREATE OR REPLACE FUNCTION FN_GET_USER_BY_EMAIL(
    p_email IN VARCHAR2
)
RETURN SYS_REFCURSOR
IS
    v_cursor SYS_REFCURSOR;
BEGIN
    OPEN v_cursor FOR
        SELECT Id, Email 
        FROM Users 
        WHERE Email = p_email;

    RETURN v_cursor;
END FN_GET_USER_BY_EMAIL;
/

-- Function: Get All Users
CREATE OR REPLACE FUNCTION FN_GET_ALL_USERS
RETURN SYS_REFCURSOR
IS
    v_cursor SYS_REFCURSOR;
BEGIN
    OPEN v_cursor FOR
        SELECT Id, Email 
        FROM Users 
        ORDER BY Email;

    RETURN v_cursor;
END FN_GET_ALL_USERS;
/

-- Function: Get Paged Users
CREATE OR REPLACE FUNCTION FN_GET_PAGED_USERS(
    p_offset IN NUMBER,
    p_page_size IN NUMBER
)
RETURN SYS_REFCURSOR
IS
    v_cursor SYS_REFCURSOR;
BEGIN
    OPEN v_cursor FOR
        SELECT Id, Email 
        FROM Users 
        ORDER BY Email
        OFFSET p_offset ROWS FETCH NEXT p_page_size ROWS ONLY;

    RETURN v_cursor;
END FN_GET_PAGED_USERS;
/

-- Function: Check if User Exists by ID
CREATE OR REPLACE FUNCTION FN_USER_EXISTS(
    p_id IN VARCHAR2
)
RETURN NUMBER
IS
    v_count NUMBER;
BEGIN
    SELECT COUNT(1) 
    INTO v_count
    FROM Users 
    WHERE Id = p_id;

    RETURN v_count;
END FN_USER_EXISTS;
/

-- Function: Check if Email Exists
CREATE OR REPLACE FUNCTION FN_EMAIL_EXISTS(
    p_email IN VARCHAR2
)
RETURN NUMBER
IS
    v_count NUMBER;
BEGIN
    SELECT COUNT(1) 
    INTO v_count
    FROM Users 
    WHERE Email = p_email;

    RETURN v_count;
END FN_EMAIL_EXISTS;
/

-- Function: Get Total User Count
CREATE OR REPLACE FUNCTION FN_GET_USER_COUNT
RETURN NUMBER
IS
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) 
    INTO v_count
    FROM Users;

    RETURN v_count;
END FN_GET_USER_COUNT;
/

-- ============================================
-- STEP 4: Command Procedures (WRITE Operations)
-- ============================================

-- Procedure: Insert User
CREATE OR REPLACE PROCEDURE SP_INSERT_USER(
    p_id IN VARCHAR2,
    p_email IN VARCHAR2
)
IS
BEGIN
    INSERT INTO Users (Id, Email) 
    VALUES (p_id, p_email);

    -- No explicit COMMIT - handled by UnitOfWork
EXCEPTION
    WHEN DUP_VAL_ON_INDEX THEN
        RAISE_APPLICATION_ERROR(-20001, 'User with this ID or Email already exists');
    WHEN OTHERS THEN
        RAISE_APPLICATION_ERROR(-20002, 'Error inserting user: ' || SQLERRM);
END SP_INSERT_USER;
/

-- Procedure: Update User
CREATE OR REPLACE PROCEDURE SP_UPDATE_USER(
    p_id IN VARCHAR2,
    p_email IN VARCHAR2
)
IS
    v_count NUMBER;
BEGIN
    -- Check if user exists
    SELECT COUNT(1) INTO v_count FROM Users WHERE Id = p_id;

    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20003, 'User not found');
    END IF;

    UPDATE Users 
    SET Email = p_email 
    WHERE Id = p_id;

    -- No explicit COMMIT - handled by UnitOfWork
EXCEPTION
    WHEN DUP_VAL_ON_INDEX THEN
        RAISE_APPLICATION_ERROR(-20004, 'Email already exists');
    WHEN OTHERS THEN
        RAISE_APPLICATION_ERROR(-20005, 'Error updating user: ' || SQLERRM);
END SP_UPDATE_USER;
/

-- Procedure: Delete User
CREATE OR REPLACE PROCEDURE SP_DELETE_USER(
    p_id IN VARCHAR2
)
IS
    v_count NUMBER;
BEGIN
    -- Check if user exists
    SELECT COUNT(1) INTO v_count FROM Users WHERE Id = p_id;

    IF v_count = 0 THEN
        RAISE_APPLICATION_ERROR(-20006, 'User not found');
    END IF;

    DELETE FROM Users 
    WHERE Id = p_id;

    -- No explicit COMMIT - handled by UnitOfWork
EXCEPTION
    WHEN OTHERS THEN
        RAISE_APPLICATION_ERROR(-20007, 'Error deleting user: ' || SQLERRM);
END SP_DELETE_USER;
/

-- ============================================
-- STEP 5: Sample Test Data (Optional)
-- ============================================

-- Insert sample users for testing
-- EXEC SP_INSERT_USER('550e8400-e29b-41d4-a716-446655440000', 'john.doe@example.com');
-- EXEC SP_INSERT_USER('550e8400-e29b-41d4-a716-446655440001', 'jane.smith@example.com');
-- EXEC SP_INSERT_USER('550e8400-e29b-41d4-a716-446655440002', 'bob.johnson@example.com');

-- ============================================
-- STEP 6: Grant Permissions (if needed)
-- ============================================

-- Grant execute permissions to application user
-- GRANT EXECUTE ON FN_GET_USER_BY_ID TO your_app_user;
-- GRANT EXECUTE ON FN_GET_USER_BY_EMAIL TO your_app_user;
-- GRANT EXECUTE ON FN_GET_ALL_USERS TO your_app_user;
-- GRANT EXECUTE ON FN_GET_PAGED_USERS TO your_app_user;
-- GRANT EXECUTE ON FN_USER_EXISTS TO your_app_user;
-- GRANT EXECUTE ON FN_EMAIL_EXISTS TO your_app_user;
-- GRANT EXECUTE ON FN_GET_USER_COUNT TO your_app_user;
-- GRANT EXECUTE ON SP_INSERT_USER TO your_app_user;
-- GRANT EXECUTE ON SP_UPDATE_USER TO your_app_user;
-- GRANT EXECUTE ON SP_DELETE_USER TO your_app_user;

-- ============================================
-- STEP 7: Verification Queries
-- ============================================

-- Test Get User by ID
-- DECLARE
--     v_cursor SYS_REFCURSOR;
--     v_id VARCHAR2(36);
--     v_email VARCHAR2(255);
-- BEGIN
--     v_cursor := FN_GET_USER_BY_ID('550e8400-e29b-41d4-a716-446655440000');
--     FETCH v_cursor INTO v_id, v_email;
--     DBMS_OUTPUT.PUT_LINE('ID: ' || v_id || ', Email: ' || v_email);
--     CLOSE v_cursor;
-- END;
-- /

-- Test Email Exists
-- SELECT FN_EMAIL_EXISTS('john.doe@example.com') AS EmailExists FROM DUAL;

-- Test Get User Count
-- SELECT FN_GET_USER_COUNT() AS TotalUsers FROM DUAL;

-- Test Get All Users
-- DECLARE
--     v_cursor SYS_REFCURSOR;
-- BEGIN
--     v_cursor := FN_GET_ALL_USERS();
--     -- Process cursor results
-- END;
-- /

-- ============================================
-- STEP 8: Cleanup Scripts (if needed)
-- ============================================

-- Drop all procedures and functions
-- DROP FUNCTION FN_GET_USER_BY_ID;
-- DROP FUNCTION FN_GET_USER_BY_EMAIL;
-- DROP FUNCTION FN_GET_ALL_USERS;
-- DROP FUNCTION FN_GET_PAGED_USERS;
-- DROP FUNCTION FN_USER_EXISTS;
-- DROP FUNCTION FN_EMAIL_EXISTS;
-- DROP FUNCTION FN_GET_USER_COUNT;
-- DROP PROCEDURE SP_INSERT_USER;
-- DROP PROCEDURE SP_UPDATE_USER;
-- DROP PROCEDURE SP_DELETE_USER;
-- DROP PACKAGE PKG_USER_OPERATIONS;
-- DROP TABLE Users;

-- ============================================
-- End of Script
-- ============================================

