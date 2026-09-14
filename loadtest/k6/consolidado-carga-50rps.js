// Valida o requisito não-funcional central do desafio:
// "Em dias de pico, o serviço de consolidado diário recebe 50 requisições por segundo,
//  com no máximo 5% de perda de requisições."
//
// Alvo: GET /consolidado/{data} a 50 req/s constantes por 2 minutos.
//
// Como rodar: ver a seção "Testes de carga (k6)" do README (o comando docker run muda um pouco
// dependendo do terminal - bash, PowerShell ou cmd.exe).
import http from 'k6/http';
import { check, sleep } from 'k6';
import { login, authHeaders } from './lib/auth.js';

const BASE_URL_LANCAMENTOS = __ENV.BASE_URL_LANCAMENTOS || 'http://lancamentos-api:8080';
const BASE_URL_CONSOLIDADO = __ENV.BASE_URL_CONSOLIDADO || 'http://consolidado-api:8080';
const SEED_USERNAME = __ENV.SEED_USERNAME || 'comerciante';
const SEED_PASSWORD = __ENV.SEED_PASSWORD || 'TrocarEssaSenha!123';

// Data fixa e exclusiva deste teste, para não colidir com dados de outra rodada/teste.
const DATA_TESTE = __ENV.DATA_TESTE || '2030-06-15';

export const options = {
    scenarios: {
        consolidado_50rps: {
            executor: 'constant-arrival-rate',
            rate: 50,
            timeUnit: '1s',
            duration: '2m',
            preAllocatedVUs: 60,
            maxVUs: 200,
        },
    },
    thresholds: {
        // O requisito do desafio: no máximo 5% de perda de requisições.
        http_req_failed: ['rate<0.05'],
    },
};

export function setup() {
    const token = login(BASE_URL_LANCAMENTOS, SEED_USERNAME, SEED_PASSWORD);

    // Garante que a data consultada durante a carga já tem saldo consolidado (senão todo
    // request cairia em 404, o que não representa o cenário real do relatório em uso).
    const criar = http.post(
        `${BASE_URL_LANCAMENTOS}/lancamentos`,
        JSON.stringify({ data: DATA_TESTE, tipo: 1, valor: 500, descricao: 'Seed para teste de carga 50 req/s' }),
        authHeaders(token),
    );
    if (criar.status !== 201) {
        throw new Error(`Falha ao semear lançamento: HTTP ${criar.status} - ${criar.body}`);
    }

    // Espera a consolidação assíncrona (outbox -> RabbitMQ -> consumer) processar o evento.
    for (let tentativas = 0; tentativas < 30; tentativas++) {
        const resp = http.get(`${BASE_URL_CONSOLIDADO}/consolidado/${DATA_TESTE}`, authHeaders(token));
        if (resp.status === 200) break;
        sleep(1);
    }

    return { token };
}

export default function (data) {
    const res = http.get(`${BASE_URL_CONSOLIDADO}/consolidado/${DATA_TESTE}`, authHeaders(data.token));
    check(res, { 'status 200': (r) => r.status === 200 });
}
