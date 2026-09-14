# ADR 0006 — Autenticação JWT, usuário único seedado, senha com BCrypt + pepper

## Status
Aceita

## Contexto
O desafio pede mecanismos de segurança/autenticação. O domínio descrito é um único comerciante
controlando seu próprio caixa — não há requisito de cadastro de múltiplos usuários, papéis ou
organizações.

## Decisão
- Um único usuário é **seedado** na inicialização do serviço de Lançamentos (tabela `users`),
  com usuário/senha configuráveis via variáveis de ambiente (`SeedUser__Username`,
  `SeedUser__Password`). Não existe endpoint de cadastro de usuário — construir essa feature seria
  over-engineering para um domínio de usuário único.
- `POST /auth/login` (em Lançamentos) valida a senha e emite um **JWT** assinado com HMAC-SHA256.
- O Consolidado **não** emite tokens nem conhece a tabela de usuários — ele só valida a assinatura,
  issuer e audience do token (mesma chave, compartilhada via configuração). Isso mantém os dois
  serviços desacoplados mesmo no aspecto de autenticação.
- A senha é armazenada com **BCrypt** (que já gera e embute um *salt* único por usuário no hash)
  mais um **pepper**: um segredo único da aplicação inteira, concatenado à senha antes do hash,
  guardado em configuração/segredo (nunca no banco). Isso é uma camada extra de defesa: se o banco
  vazar sem que a configuração/segredos vazem junto, o hash sozinho não é suficiente.

## Alternativas consideradas
- **API Key estática**: mais simples, mas menos representativo dos mecanismos de segurança que o
  desafio pede para demonstrar (autenticação, não só autorização binária).
- **Identity Provider externo (Keycloak/IdentityServer/Auth0/Azure AD B2C) com OAuth2/OIDC**: é o
  caminho correto para um sistema real multiusuário, mas desproporcional ao escopo de um único
  usuário neste desafio. Registrado como evolução futura no README.

## Consequências
- Simplicidade: não há fluxo de registro, recuperação de senha, etc. — fora de escopo aqui.
- Os segredos (chave de assinatura do JWT, pepper) vêm de variáveis de ambiente/`.env` local; em
  produção iriam para um vault (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault).
