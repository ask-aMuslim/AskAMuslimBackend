# Quick Deployment Reference

## 🚀 Deploy to Production

### Method 1: Automatic (Recommended)
```bash
# Merge your changes to production branch
git checkout production
git merge main  # or your feature branch
git push origin production

# GitHub Actions will automatically deploy! 🎉
# View progress at: https://github.com/ask-aMuslim/AskAMuslimBackend/actions
```

### Method 2: Manual Deployment
```bash
# SSH into server
ssh azmy@vmi3084818

# Run deployment script
cd /srv/docker/api/AskAMuslimBackend/.github/workflows
./deploy-manual.sh
```

### Method 3: Direct Commands
```bash
# SSH into server
ssh azmy@vmi3084818

# Pull and rebuild
cd /srv/docker/api/AskAMuslimBackend && git pull origin production
cd /srv/docker && docker compose build api && docker compose up -d api
```

## 📊 Check Deployment Status

```bash
# On server
docker compose ps api
docker compose logs api --tail 50 -f

# Check health
docker inspect api --format='{{.State.Health.Status}}'
```

## 🔧 Setup Required (One-time)

### 1. Add GitHub Secrets
Go to: https://github.com/ask-aMuslim/AskAMuslimBackend/settings/secrets/actions

Add these secrets:
- `SERVER_HOST`: vmi3084818 (or your server IP)
- `SERVER_USER`: azmy
- `SSH_PRIVATE_KEY`: Your deployment SSH private key
- `SSH_PORT`: 22 (optional)

### 2. Generate SSH Key (if needed)
```bash
ssh-keygen -t ed25519 -C "github-deploy" -f ~/.ssh/github_deploy
ssh-copy-id -i ~/.ssh/github_deploy.pub azmy@vmi3084818
# Then add private key to GitHub Secrets
```

## 🆘 Troubleshooting

### Deployment Failed
```bash
# Check GitHub Actions logs first!
# Then SSH and check:
docker compose logs api --tail 200
docker compose ps api
docker system df  # Check disk space
```

### Rollback
```bash
ssh azmy@vmi3084818
cd /srv/docker/api/AskAMuslimBackend
git log --oneline -10  # Find commit to rollback to
git reset --hard <commit-hash>
cd /srv/docker
docker compose build api && docker compose up -d api
```

## 📝 Pre-deployment Checklist

- [ ] Changes tested locally
- [ ] Database migrations ready (if any)
- [ ] Environment variables updated (if needed)
- [ ] Dependencies updated in Dockerfile
- [ ] Tests passing (`dotnet test`)
- [ ] Code reviewed and approved
- [ ] Merged to production branch

## 🔗 Useful Links

- Repository: https://github.com/ask-aMuslim/AskAMuslimBackend
- Actions: https://github.com/ask-aMuslim/AskAMuslimBackend/actions
- Server: ssh://azmy@vmi3084818
- API Endpoint: http://vmi3084818/api (or your domain)
