// Authentication helper functions
const AuthHelper = {
    // Kiểm tra và cập nhật trạng thái đăng nhập trong UI
    async updateAuthUI() {
        const isAuthenticated = ApiService.isAuthenticated();
        const loginLinks = document.querySelectorAll('.auth-login');
        const logoutLinks = document.querySelectorAll('.auth-logout');
        const profileLinks = document.querySelectorAll('.auth-profile');
        const customerLinks = document.querySelectorAll('.auth-customer');
        const adminLinks = document.querySelectorAll('.auth-admin');

        if (isAuthenticated) {
            loginLinks.forEach(link => link.style.display = 'none');
            logoutLinks.forEach(link => link.style.display = 'block');
            profileLinks.forEach(link => link.style.display = 'block');

            // Load user info và check roles
            await this.loadUserInfo();
        } else {
            loginLinks.forEach(link => link.style.display = 'block');
            logoutLinks.forEach(link => link.style.display = 'none');
            profileLinks.forEach(link => link.style.display = 'none');
            customerLinks.forEach(link => link.style.display = 'none');
            adminLinks.forEach(link => link.style.display = 'none');
        }
    },

    // Load thông tin user
    async loadUserInfo() {
        try {
            const user = await ApiService.getCurrentUser();
            const userInfoElements = document.querySelectorAll('.user-email');
            userInfoElements.forEach(el => {
                el.textContent = user.email;
            });

            // Check và hiển thị menu dựa trên role
            const roleName = user.roleName || '';
            const isAdmin = roleName === 'Admin' || roleName === 'admin';
            const isCustomer = roleName === 'Customer' || roleName === 'customer' || !isAdmin;

            const adminLinks = document.querySelectorAll('.auth-admin');
            adminLinks.forEach(link => {
                link.style.display = isAdmin ? 'block' : 'none';
            });

            const customerLinks = document.querySelectorAll('.auth-customer');
            customerLinks.forEach(link => {
                link.style.display = isCustomer ? 'block' : 'none';
            });
        } catch (error) {
            console.error('Không thể load thông tin user:', error);
            const adminLinks = document.querySelectorAll('.auth-admin');
            const customerLinks = document.querySelectorAll('.auth-customer');
            adminLinks.forEach(link => link.style.display = 'none');
            customerLinks.forEach(link => link.style.display = 'none');
        }
    },

    // Xử lý đăng xuất
    handleLogout() {
        if (confirm('Bạn có chắc muốn đăng xuất?')) {
            ApiService.logout();
        }
    }
};

// Khởi tạo khi trang load
document.addEventListener('DOMContentLoaded', async function () {
    await AuthHelper.updateAuthUI();
});

// Export
window.AuthHelper = AuthHelper;