// API Service để gọi các API từ backend
const ApiService = {
    baseUrl: 'http://localhost:5209/api',

    // Lấy token từ localStorage
    getToken() {
        return localStorage.getItem('authToken');
    },

    // Lưu token vào localStorage
    setToken(token) {
        localStorage.setItem('authToken', token);
    },

    // Xóa token
    removeToken() {
        localStorage.removeItem('authToken');
    },

    // Kiểm tra đã đăng nhập chưa
    isAuthenticated() {
        return !!this.getToken();
    },

    // Tạo headers cho request
    getHeaders(includeAuth = true) {
        const headers = {
            'Content-Type': 'application/json'
        };

        if (includeAuth && this.isAuthenticated()) {
            headers['Authorization'] = `Bearer ${this.getToken()}`;
        }

        return headers;
    },

    // Xử lý response
    async handleResponse(response) {
        const contentType = response.headers.get('content-type');
        let data;

        if (contentType && contentType.includes('application/json')) {
            data = await response.json();
        } else {
            data = await response.text();
        }

        if (!response.ok) {
            const error = data.message || data || `HTTP error! status: ${response.status}`;
            throw new Error(error);
        }

        return data;
    },

    // ============ AUTH APIs ============

    // Đăng ký
    async register(email, password) {
        try {
            const formData = new FormData();
            formData.append('Email', email);
            formData.append('Password', password);

            const response = await fetch(`${this.baseUrl}/Auth/register`, {
                method: 'POST',
                body: formData
            });

            return this.handleResponse(response);
        } catch (error) {
            // Xử lý lỗi network
            if (error.message.includes('Failed to fetch') || error.message.includes('NetworkError')) {
                throw new Error('Không thể kết nối đến server. Vui lòng kiểm tra backend đã được khởi động chưa.');
            }
            throw error;
        }
    },

    // Đăng nhập
    async login(email, password) {
        try {
            const formData = new FormData();
            formData.append('Email', email);
            formData.append('Password', password);

            const response = await fetch(`${this.baseUrl}/Auth/login`, {
                method: 'POST',
                body: formData
            });

            const data = await this.handleResponse(response);
            if (data.token || data.Token) {
                this.setToken(data.token || data.Token);
            }
            return data;
        } catch (error) {
            // Xử lý lỗi network (CORS, connection refused, etc.)
            if (error.message.includes('Failed to fetch') || error.message.includes('NetworkError')) {
                throw new Error('Không thể kết nối đến server. Vui lòng kiểm tra:\n1. Backend đã được khởi động chưa?\n2. URL API có đúng không?\n3. CORS đã được cấu hình đúng chưa?');
            }
            throw error;
        }
    },

    // Đăng xuất
    logout() {
        this.removeToken();
        window.location.href = '/Home/Index';
    },

    // Xác thực email
    async verifyEmail(email, code) {
        const formData = new FormData();
        formData.append('Email', email);
        formData.append('Code', code);

        const response = await fetch(`${this.baseUrl}/Auth/verify-email`, {
            method: 'POST',
            body: formData
        });

        return this.handleResponse(response);
    },

    // Quên mật khẩu
    async forgotPassword(email) {
        const formData = new FormData();
        formData.append('Email', email);

        const response = await fetch(`${this.baseUrl}/Auth/forgot-password`, {
            method: 'POST',
            body: formData
        });

        return this.handleResponse(response);
    },

    // Đặt lại mật khẩu
    async resetPassword(email, token, newPassword) {
        const formData = new FormData();
        formData.append('Email', email);
        formData.append('Token', token);
        formData.append('NewPassword', newPassword);

        const response = await fetch(`${this.baseUrl}/Auth/reset-password`, {
            method: 'POST',
            body: formData
        });

        return this.handleResponse(response);
    },

    // Lấy thông tin user hiện tại
    async getCurrentUser() {
        const response = await fetch(`${this.baseUrl}/Auth/me`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // ============ PROFILE APIs ============

    // Lấy profile của mình
    async getMyProfile() {
        const response = await fetch(`${this.baseUrl}/Profiles/me`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Cập nhật profile
    async updateProfile(profileData) {
        const formData = new FormData();
        if (profileData.knownAs) formData.append('KnownAs', profileData.knownAs);
        if (profileData.dateOfBirth) formData.append('DateOfBirth', profileData.dateOfBirth);
        if (profileData.gender) formData.append('Gender', profileData.gender);
        if (profileData.bio) formData.append('Bio', profileData.bio);
        if (profileData.city) formData.append('City', profileData.city);
        if (profileData.country) formData.append('Country', profileData.country);
        if (profileData.lookingForGender) formData.append('LookingForGender', profileData.lookingForGender);

        const response = await fetch(`${this.baseUrl}/Profiles/me`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${this.getToken()}`
            },
            body: formData
        });

        return this.handleResponse(response);
    },

    // Lấy danh sách gợi ý
    async getSuggestions(take = 20) {
        const response = await fetch(`${this.baseUrl}/Profiles/suggestions?take=${take}`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Lấy profile theo userId
    async getProfileByUserId(userId) {
        const response = await fetch(`${this.baseUrl}/Profiles/${userId}`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // ============ ADMIN APIs ============

    // Kiểm tra user có phải admin không
    async checkAdminRole() {
        try {
            const user = await this.getCurrentUser();
            return user.roleName === 'Admin' || user.roleName === 'admin';
        } catch (error) {
            return false;
        }
    },

    // Lấy danh sách tất cả users (admin only)
    async getAllUsers(skip = 0, take = 50) {
        const response = await fetch(`${this.baseUrl}/Admin/users?skip=${skip}&take=${take}`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Khóa user (admin only)
    async banUser(userId) {
        const response = await fetch(`${this.baseUrl}/Admin/ban-user/${userId}`, {
            method: 'POST',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Mở khóa user (admin only)
    async unbanUser(userId) {
        const response = await fetch(`${this.baseUrl}/Admin/unban-user/${userId}`, {
            method: 'POST',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Lấy thống kê (admin only)
    async getStatistics() {
        const response = await fetch(`${this.baseUrl}/Admin/statistics`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // ============ ANALYTICS APIs (Admin) ============

    // Lấy tỷ lệ match (admin only)
    async getMatchRate() {
        const response = await fetch(`${this.baseUrl}/Analytics/match-rate`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Lấy danh sách users hoạt động (admin only)
    async getActiveUsers(days = 7, take = 50) {
        const response = await fetch(`${this.baseUrl}/Analytics/active-users?days=${days}&take=${take}`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // ============ INTERESTS APIs ============

    // Lấy tất cả interests
    async getAllInterests() {
        const response = await fetch(`${this.baseUrl}/Interests`, {
            method: 'GET',
            headers: this.getHeaders(false)
        });

        return this.handleResponse(response);
    },

    // Tạo interest mới (admin only)
    async createInterest(name) {
        const response = await fetch(`${this.baseUrl}/Interests`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.getToken()}`
            },
            body: JSON.stringify({ name })
        });

        return this.handleResponse(response);
    },

    // ============ CUSTOMER APIs ============

    // ============ LIKES APIs ============

    // Like một user
    async likeUser(targetUserId) {
        const response = await fetch(`${this.baseUrl}/Likes/${targetUserId}`, {
            method: 'POST',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Lấy danh sách người mình đã like
    async getMyLikes() {
        const response = await fetch(`${this.baseUrl}/Likes`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Lấy danh sách người đã like mình
    async getLikedBy() {
        const response = await fetch(`${this.baseUrl}/Likes/liked-by`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // ============ MATCHES APIs ============

    // Lấy danh sách matches
    async getMyMatches() {
        const response = await fetch(`${this.baseUrl}/Matches`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Xóa match
    async deleteMatch(matchId) {
        const response = await fetch(`${this.baseUrl}/Matches/${matchId}`, {
            method: 'DELETE',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // ============ CONVERSATIONS APIs ============

    // Lấy danh sách conversations
    async getConversations() {
        const response = await fetch(`${this.baseUrl}/Conversations`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Lấy conversation theo id
    async getConversationById(conversationId) {
        const response = await fetch(`${this.baseUrl}/Conversations/${conversationId}`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Tạo conversation mới
    async createConversation(user2Id) {
        const response = await fetch(`${this.baseUrl}/Conversations`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.getToken()}`
            },
            body: JSON.stringify({ user2Id })
        });

        return this.handleResponse(response);
    },

    // ============ MESSAGES APIs ============

    // Lấy messages trong conversation
    async getMessages(conversationId, skip = 0, take = 50) {
        const response = await fetch(`${this.baseUrl}/Messages/${conversationId}?skip=${skip}&take=${take}`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Gửi message
    async sendMessage(conversationId, recipientId, content) {
        const response = await fetch(`${this.baseUrl}/Messages`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.getToken()}`
            },
            body: JSON.stringify({
                conversationId,
                recipientId,
                content
            })
        });

        return this.handleResponse(response);
    },

    // Đánh dấu message đã đọc
    async markMessageAsRead(messageId) {
        const response = await fetch(`${this.baseUrl}/Messages/${messageId}/read`, {
            method: 'PUT',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Xóa message
    async deleteMessage(messageId) {
        const response = await fetch(`${this.baseUrl}/Messages/${messageId}`, {
            method: 'DELETE',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // ============ PHOTOS APIs ============

    // Upload photo
    async uploadPhoto(file, photoType = null) {
        const formData = new FormData();
        formData.append('File', file);
        if (photoType) formData.append('PhotoType', photoType);

        const response = await fetch(`${this.baseUrl}/Photos`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.getToken()}`
            },
            body: formData
        });

        return this.handleResponse(response);
    },

    // Lấy photos của user
    async getPhotosByUser(userId) {
        const response = await fetch(`${this.baseUrl}/Photos/${userId}`, {
            method: 'GET',
            headers: this.getHeaders(false)
        });

        return this.handleResponse(response);
    },

    // Đặt ảnh chính
    async setMainPhoto(photoId) {
        const response = await fetch(`${this.baseUrl}/Photos/${photoId}/set-main`, {
            method: 'PUT',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Xóa photo
    async deletePhoto(photoId) {
        const response = await fetch(`${this.baseUrl}/Photos/${photoId}`, {
            method: 'DELETE',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // ============ USER INTERESTS APIs ============

    // Cập nhật sở thích của user
    async updateMyInterests(interestIds) {
        const response = await fetch(`${this.baseUrl}/users/interests`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.getToken()}`
            },
            body: JSON.stringify({ interestIds })
        });

        return this.handleResponse(response);
    },

    // Lấy sở thích của user
    async getUserInterests(userId) {
        const response = await fetch(`${this.baseUrl}/users/interests/${userId}`, {
            method: 'GET',
            headers: this.getHeaders(false)
        });

        return this.handleResponse(response);
    },

    // ============ RECOMMENDATIONS APIs ============

    // Lấy recommendations
    async getRecommendations(take = 20) {
        const response = await fetch(`${this.baseUrl}/Recommendations?take=${take}`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // ============ BLOCKS APIs ============

    // Chặn user
    async blockUser(targetUserId) {
        const response = await fetch(`${this.baseUrl}/Blocks/${targetUserId}`, {
            method: 'POST',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Lấy danh sách người đã bị chặn
    async getBlockedUsers() {
        const response = await fetch(`${this.baseUrl}/Blocks`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Bỏ chặn user
    async unblockUser(targetUserId) {
        const response = await fetch(`${this.baseUrl}/Blocks/${targetUserId}`, {
            method: 'DELETE',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // ============ USERS APIs ============

    // Lấy danh sách users (có phân trang)
    async getUsers(skip = 0, take = 50) {
        const response = await fetch(`${this.baseUrl}/Users?skip=${skip}&take=${take}`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Lấy user theo id
    async getUserById(userId) {
        const response = await fetch(`${this.baseUrl}/Users/${userId}`, {
            method: 'GET',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Xóa user (có thể cần quyền admin)
    async deleteUser(userId) {
        const response = await fetch(`${this.baseUrl}/Users/${userId}`, {
            method: 'DELETE',
            headers: this.getHeaders()
        });

        return this.handleResponse(response);
    },

    // Cập nhật trạng thái user (Active, Inactive, Hidden, etc.)
    async updateUserStatus(status) {
        const formData = new FormData();
        formData.append('Status', status);

        const response = await fetch(`${this.baseUrl}/Users/status`, {
            method: 'PATCH',
            headers: {
                'Authorization': `Bearer ${this.getToken()}`
            },
            body: formData
        });

        return this.handleResponse(response);
    }
};

// Export để sử dụng global
window.ApiService = ApiService;