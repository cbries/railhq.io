# Security Policy

## 🔒 Supported Versions

We release patches for security vulnerabilities for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| Latest  | :white_check_mark: |
| < Latest| :x:                |

## 🚨 Reporting a Vulnerability

If you discover a security vulnerability, please follow these steps:

### DO NOT

- ❌ Open a public GitHub issue
- ❌ Disclose the vulnerability publicly before it's fixed
- ❌ Share the vulnerability with third parties

### DO

1. **Email the maintainer directly**: mail@cbries.de
2. **Provide details**:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if you have one)

### Response Timeline

- **Initial Response**: Within 48 hours
- **Status Update**: Within 7 days
- **Fix Timeline**: Depends on severity
  - Critical: 1-7 days
  - High: 7-30 days
  - Medium: 30-90 days
  - Low: 90+ days

## 🛡️ Security Best Practices

### For Users

1. **Change Default Credentials**
   ```
   Default Username: free@railhq.io
   Default Password: railhq.io
   ```
   ⚠️ **Change these immediately after first login!**

2. **Use HTTPS in Production**
   - Enable TLS/HTTPS for production deployments
   - Never expose the system to the internet with HTTP only

3. **Keep Software Updated**
   - Regularly update to the latest version
   - Subscribe to security announcements

4. **Network Security**
   - Run on private/local networks only (unless properly secured)
   - Use firewall rules to restrict access
   - Consider VPN for remote access

5. **Secure Redis**
   - Redis should only be accessible from localhost
   - Use Redis authentication if exposed to network

### For Developers

1. **Never Commit Secrets**
   - No passwords, API keys, or tokens in code
   - Use environment variables for sensitive data
   - Review `.gitignore` before committing

2. **Validate Input**
   - Sanitize all user input
   - Validate file uploads
   - Use parameterized queries

3. **Authentication & Authorization**
   - Implement proper session management
   - Use secure password hashing (already using SHA256 + salt)
   - Implement rate limiting for login attempts

4. **Dependencies**
   - Regularly update NuGet packages
   - Review security advisories
   - Use Dependabot (enabled in this project)

## 🔐 Secure Configuration

### Environment Variables

Never commit these files:
- `.env` (only `.env.example` should be in repo)
- `*.pfx` certificate files
- `users.json` with real user data
- Configuration files with real credentials

### Default Files to Review

Before deployment, review and customize:

1. **`resources/users.json`**
   - Change default user password
   - Remove demo users in production

2. **`Gateway/railyGateway/railhqGateway.json`**
   - Update connection credentials
   - Configure command station IPs

3. **`.env`**
   - Set proper data directories
   - Configure TLS settings
   - Set secure Redis configuration

## 🚀 Secure Deployment Checklist

- [ ] Change all default passwords
- [ ] Enable HTTPS/TLS
- [ ] Configure firewall rules
- [ ] Secure Redis with password
- [ ] Review and update `.env` configuration
- [ ] Disable debug mode in production
- [ ] Set up regular backups
- [ ] Configure proper logging
- [ ] Implement monitoring/alerting
- [ ] Document your security setup

## 📝 Security Updates

Security updates will be announced via:
- GitHub Security Advisories
- Release notes
- Project README

## 🙏 Acknowledgments

We appreciate responsible disclosure and will acknowledge security researchers who help us improve railhq.io's security.

---

**Remember**: Security is a shared responsibility. Help us keep railhq.io secure for everyone!
