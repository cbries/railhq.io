# Contributing to railhq.io

Thank you for your interest in contributing to railhq.io! This document provides guidelines and information for contributors.

## 🎯 Ways to Contribute

- **Report Bugs** - Found a bug? Open an issue!
- **Suggest Features** - Have an idea? We'd love to hear it!
- **Submit Pull Requests** - Fix bugs or add features
- **Improve Documentation** - Help make our docs better
- **Test** - Help test new features and report issues
- **Translate** - Help translate the interface

## 🚀 Getting Started

### Prerequisites

- Git
- .NET 8.0 SDK
- Docker and Docker Compose (for testing)
- VS Code (recommended) or your preferred IDE

### Development Setup

1. **Fork the repository** on GitHub

2. **Clone your fork:**
   ```bash
   git clone https://github.com/YOUR_USERNAME/railhq.io.git
   cd railhq.io
   ```

3. **Add upstream remote:**
   ```bash
   git remote add upstream https://github.com/ORIGINAL_ORG/railhq.io.git
   ```

4. **Set up development environment:**
   ```bash
   # Start Redis
   docker compose -f docker-compose-redis.yml up -d
   
   # Restore dependencies
   dotnet restore
   
   # Build
   dotnet build
   ```

5. **Run the applications:**
   ```bash
   # Terminal 1: railyWebIndex
   cd WebApp/railyWebIndex && dotnet run
   
   # Terminal 2: railyWebApp
   cd WebApp/railyWebApp && dotnet run
   ```

## 📝 Pull Request Process

### Before Starting

1. **Check existing issues** - Someone might already be working on it
2. **Create an issue** - For larger changes, discuss first
3. **Sync your fork** - Make sure you have the latest changes

### Creating a Pull Request

1. **Create a feature branch:**
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/your-bug-fix
   ```

2. **Make your changes:**
   - Write clean, readable code
   - Follow existing code style
   - Add comments where necessary
   - Update documentation if needed

3. **Test your changes:**
   ```bash
   # Build to check for errors
   dotnet build
   
   # Test with Docker
   ./docker-build_and_export.sh
   docker compose up -d
   
   # Build Gateway for your platform
   ./build-gateway-local.sh linux-x64
   ```

4. **Commit your changes:**
   ```bash
   git add .
   git commit -m "feat: add amazing new feature"
   # or
   git commit -m "fix: resolve issue with login"
   ```

5. **Push and create PR:**
   ```bash
   git push origin feature/your-feature-name
   ```
   Then open a Pull Request on GitHub.

### Commit Message Format

We follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>: <description>

[optional body]

[optional footer]
```

**Types:**
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `style:` - Code style changes (formatting, etc.)
- `refactor:` - Code refactoring
- `test:` - Adding or updating tests
- `chore:` - Maintenance tasks

**Examples:**
```
feat: add support for z21 feedback modules
fix: resolve WebSocket connection timeout
docs: update installation guide for Docker
```

## 🏗️ Project Structure

```
railhq.io/
├── Gateway/                    # Gateway application
│   ├── railyGateway/          # Main gateway logic
│   ├── railyEsuEcos/          # ESU ECoS protocol driver
│   ├── railhqZ21/             # z21 protocol driver
│   ├── railyHsi88Usb/         # HSI-88-USB driver
│   └── railySystray/          # System tray application
├── WebApp/
│   ├── railyWebIndex/         # User management, login
│   └── railyWebApp/           # Main web application
├── Libraries/
│   ├── libShared/             # Shared utilities
│   ├── libMetamodel/          # Data models
│   ├── libUserspace/          # User-related functionality
│   └── ...                    # Other libraries
├── resources/                  # Themes, demo data
├── resourcesRuntime/          # Runtime configurations
└── WebDoc/                    # Documentation (Docusaurus)
```

## 🎨 Code Style

### C# Code Style

- Use meaningful variable and method names
- Follow Microsoft's [C# coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `var` when the type is obvious
- Prefer expression-bodied members for simple methods/properties
- Use XML documentation for public APIs

### JavaScript Code Style

- Use meaningful variable and function names
- Prefer `const` over `let`, avoid `var`
- Use template literals for string interpolation
- Add JSDoc comments for functions

## 🧪 Testing

### Manual Testing

1. Test with different browsers (Chrome, Firefox, Edge)
2. Test on different screen sizes
3. Test with different command stations if possible

### Docker Testing

```bash
# Build and test complete system
docker compose build railhq
docker compose up -d

# Check logs for errors
docker compose logs -f
```

## 📚 Documentation

- Update README.md for user-facing changes
- Update INSTALL.md for installation-related changes
- Add inline code comments for complex logic
- Update JSDoc/XML documentation for APIs

## 🐛 Reporting Bugs

When reporting bugs, please include:

1. **Description** - What happened?
2. **Expected behavior** - What should have happened?
3. **Steps to reproduce** - How can we reproduce it?
4. **Environment:**
   - OS and version
   - Browser and version
   - Docker version (if applicable)
   - Command station model (if relevant)
5. **Screenshots/Logs** - If applicable

## 💡 Feature Requests

For feature requests, please include:

1. **Use case** - Why do you need this feature?
2. **Proposed solution** - How do you envision it working?
3. **Alternatives** - Have you considered other approaches?

## 📜 Code of Conduct

- Be respectful and inclusive
- Provide constructive feedback
- Help others learn and grow
- Focus on what's best for the community

## 📄 License

By contributing, you agree that your contributions will be licensed under the same license as the project.

## 🙏 Thank You!

Every contribution helps make railhq.io better for the entire model railway community. Thank you for being part of it!
