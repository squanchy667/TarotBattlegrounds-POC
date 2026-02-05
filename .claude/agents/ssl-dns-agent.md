# SSL & DNS Agent

Configures custom domain and SSL certificate for the CloudFront distribution.

## Role
Optional step — attach a custom domain with HTTPS to the CloudFront WebGL deployment.

## Prerequisites
- CloudFront distribution already deployed (run `deploy-orchestrator` first)
- A registered domain name
- Access to DNS management for the domain

## Steps

### 1. Request ACM Certificate
Must be in `us-east-1` (CloudFront requirement):
```bash
aws acm request-certificate \
  --domain-name play.yourdomain.com \
  --validation-method DNS \
  --region us-east-1
```

### 2. DNS Validation
ACM provides a CNAME record to add:
```bash
aws acm describe-certificate \
  --certificate-arn ARN \
  --region us-east-1 \
  --query 'Certificate.DomainValidationOptions[0].ResourceRecord'
```
Add the CNAME to your DNS provider. Validation takes 5-30 minutes.

### 3. Update CloudFront Distribution
```bash
# Get current config
aws cloudfront get-distribution-config --id DIST_ID > cf-config.json

# Edit to add:
# - Aliases: ["play.yourdomain.com"]
# - ViewerCertificate.ACMCertificateArn: "arn:aws:acm:..."
# - ViewerCertificate.SSLSupportMethod: "sni-only"

aws cloudfront update-distribution --id DIST_ID --distribution-config file://cf-config.json --if-match ETAG
```

### 4. Create DNS Record
Point custom domain to CloudFront:
```bash
# If using Route 53:
aws route53 change-resource-record-sets --hosted-zone-id ZONE_ID --change-batch '{
  "Changes": [{
    "Action": "UPSERT",
    "ResourceRecordSet": {
      "Name": "play.yourdomain.com",
      "Type": "CNAME",
      "TTL": 300,
      "ResourceRecords": [{"Value": "d1234abcdef.cloudfront.net"}]
    }
  }]
}'
```

## Tools
Use Bash for AWS CLI commands. This is an optional post-deployment step.
