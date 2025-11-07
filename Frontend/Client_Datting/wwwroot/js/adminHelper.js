// Admin Helper để kiểm tra quyền admin và quản lý admin UI
const AdminHelper = {
    // Kiểm tra user có phải admin không
    async checkAdminAccess() {
        if (!ApiService.isAuthenticated()) {
            window.location.href = '/Auth/Login';
            return false;
        }

        try {
            const isAdmin = await ApiService.checkAdminRole();
            if (!isAdmin) {
                alert('Bạn không có quyền truy cập trang này!');
                window.location.href = '/Home/Index';
                return false;
            }
            return true;
        } catch (error) {
            console.error('Lỗi kiểm tra quyền admin:', error);
            window.location.href = '/Home/Index';
            return false;
        }
    },

    // Format date
    formatDate(dateString) {
        if (!dateString) return 'N/A';
        const date = new Date(dateString);
        return date.toLocaleString('vi-VN');
    },

    // Format status badge
    getStatusBadge(status) {
        const statusMap = {
            'Active': 'success',
            'Inactive': 'danger',
        };
        const color = statusMap[status] || 'secondary';
        return `<span class="badge bg-${color}">${status || 'N/A'}</span>`;
    },

    // Format verified badge
    getVerifiedBadge(isVerified) {
        if (isVerified) {
            return '<span class="badge bg-success">Đã xác thực</span>';
        }
        return '<span class="badge bg-warning">Chưa xác thực</span>';
    }
};

// Export
window.AdminHelper = AdminHelper;