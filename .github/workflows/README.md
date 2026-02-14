# GitHub Actions Deployment Setup

## Overview
This repository uses GitHub Actions to automatically deploy the API to production when code is pushed to the `production` branch.

## Prerequisites

### 1. GitHub Secrets Configuration
You need to add the following secrets to your GitHub repository:

**Settings → Secrets and variables → Actions → New repository secret**

| Secret Name | Description | Example Value |
|------------|-------------|---------------|
| `SERVER_HOST` | Production server hostname or IP | `vmi3084818` or your server's IP address |
| `SERVER_USER` | SSH username on the server | `azmy` |
| `SSH_PRIVATE_KEY` | SSH private key for authentication | Contents of `~/.ssh/id_rsa` or deployment key |
| `SSH_PORT` | SSH port (optional, defaults to 22) | `22` |

### 2. Generate and Add SSH Key

On your **local machine** or **GitHub Actions**, generate an SSH key pair:

```bash
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/github_deploy_key
```

**Add the public key to the server:**

```bash
# On your local machine
ssh-copy-id -i ~/.ssh/github_deploy_key.pub azmy@vmi3084818

# Or manually:
cat ~/.ssh/github_deploy_key.pub | ssh azmy@vmi3084818 "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys"
```

**Add the private key to GitHub Secrets:**

1. Copy the private key:
   ```bash
   cat ~/.ssh/github_deploy_key
   ```

2. Go to GitHub: **Settings → Secrets → Actions → New repository secret**
3. Name: `SSH_PRIVATE_KEY`
4. Value: Paste the entire private key (including `-----BEGIN` and `-----END` lines)

## Deployment Workflow

### Automatic Deployment
When you push to the `production` branch:

```bash
git checkout production
git merge main  # or your development branch
git push origin production
```

The GitHub Action will automatically:
1. ✅ Connect to the production server via SSH
2. ✅ Pull the latest code from the `production` branch
3. ✅ Rebuild the API Docker image
4. ✅ Restart the API container with zero-downtime
5. ✅ Wait for health checks to pass
6. ✅ Report deployment status

### Manual Deployment (SSH)
If you need to deploy manually:

```bash
ssh azmy@vmi3084818
cd /srv/docker/api/AskAMuslimBackend
git pull origin production
cd /srv/docker
docker compose build api
docker compose up -d api
docker compose ps api
```

## Monitoring Deployment

### View GitHub Actions Logs
1. Go to your repository on GitHub
2. Click **Actions** tab
3. Click on the latest workflow run
4. View logs for each step

### Check Deployment on Server

```bash
# SSH into the server
ssh azmy@vmi3084818

# Check container status
docker compose ps api

# View API logs
docker compose logs api --tail 100 -f

# Check health status
docker inspect api --format='{{.State.Health.Status}}'
```

## Troubleshooting

### Deployment Failed

1. **Check GitHub Actions logs** for error messages
2. **SSH into server** and check Docker logs:
   ```bash
   docker compose logs api --tail 200
   ```
3. **Verify disk space**:
   ```bash
   df -h
   ```
4. **Check Docker service**:
   ```bash
   sudo systemctl status docker
   ```

### Rollback to Previous Version

```bash
ssh azmy@vmi3084818
cd /srv/docker/api/AskAMuslimBackend
git log --oneline  # Find the commit hash to rollback to
git reset --hard <commit-hash>
cd /srv/docker
docker compose build api
docker compose up -d api
```

## Local Testing

Test the deployment script locally before pushing:

```bash
# On your local machine
ssh azmy@vmi3084818 'bash -s' < .github/workflows/deploy.sh
```

## Security Best Practices

1. ✅ Use deployment-specific SSH keys (not your personal keys)
2. ✅ Restrict SSH key access to specific commands (optional)
3. ✅ Rotate SSH keys periodically
4. ✅ Monitor GitHub Actions logs for unauthorized access
5. ✅ Use GitHub Environment protection rules for production deployments

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [SSH Action Documentation](https://github.com/appleboy/ssh-action)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
