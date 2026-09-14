import http from 'k6/http';
import { check } from 'k6';

// Faz login uma única vez (chamado a partir de setup(), que roda fora das VUs) e devolve o
// token JWT para ser reaproveitado por todas as VUs durante o teste - evita logar a cada
// iteração, o que poluiria os resultados do teste de carga com uma chamada que não é o alvo.
export function login(baseUrl, username, password) {
    const res = http.post(
        `${baseUrl}/auth/login`,
        JSON.stringify({ username, password }),
        { headers: { 'Content-Type': 'application/json' } },
    );

    check(res, { 'login: status 200': (r) => r.status === 200 });

    if (res.status !== 200) {
        throw new Error(`Falha no login em ${baseUrl}/auth/login: HTTP ${res.status} - ${res.body}`);
    }

    return res.json('token');
}

export function authHeaders(token) {
    return { headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } };
}
