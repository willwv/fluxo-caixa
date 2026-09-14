// Valida a hipótese: "o cache Redis (TTL curto) reduz a latência/carga no banco quando a mesma
// data é consultada repetidamente, comparado a datas que nunca estão em cache."
//
// Dois cenários rodando em paralelo:
//   - cache_hit:  sempre consulta a MESMA data (semeada no setup) -> após a 1a chamada, todo
//                 request subsequente é resolvido pelo Redis (ver RedisCache.ObterAsync).
//   - cache_miss: consulta uma data aleatória sem nenhum saldo (sempre 404) -> nunca está em
//                 cache (o repositório só grava no cache quando o saldo existe, ver
//                 SaldoDiarioReadRepository), então cada request sempre bate na read replica.
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

const DATA_CACHE_HIT = __ENV.DATA_CACHE_HIT || '2030-07-01';

export const options = {
    scenarios: {
        cache_hit: {
            executor: 'constant-arrival-rate',
            rate: 20,
            timeUnit: '1s',
            duration: '30s',
            preAllocatedVUs: 30,
            maxVUs: 60,
            exec: 'requisitarCacheHit',
        },
        cache_miss: {
            executor: 'constant-arrival-rate',
            rate: 20,
            timeUnit: '1s',
            duration: '30s',
            preAllocatedVUs: 30,
            maxVUs: 60,
            exec: 'requisitarCacheMiss',
        },
    },
    thresholds: {
        // Não são requisitos do desafio, são só instrumentação pra comparar os dois caminhos -
        // por isso não fazem o teste falhar (abortOnFail nunca é setado).
        'http_req_duration{scenario:cache_hit}': ['p(95)>=0'],
        'http_req_duration{scenario:cache_miss}': ['p(95)>=0'],
    },
};

export function setup() {
    const token = login(BASE_URL_LANCAMENTOS, SEED_USERNAME, SEED_PASSWORD);

    const criar = http.post(
        `${BASE_URL_LANCAMENTOS}/lancamentos`,
        JSON.stringify({ data: DATA_CACHE_HIT, tipo: 1, valor: 250, descricao: 'Seed para teste de cache hit' }),
        authHeaders(token),
    );
    if (criar.status !== 201) {
        throw new Error(`Falha ao semear lançamento: HTTP ${criar.status} - ${criar.body}`);
    }

    for (let tentativas = 0; tentativas < 30; tentativas++) {
        const resp = http.get(`${BASE_URL_CONSOLIDADO}/consolidado/${DATA_CACHE_HIT}`, authHeaders(token));
        if (resp.status === 200) break;
        sleep(1);
    }

    return { token };
}

export function requisitarCacheHit(data) {
    const res = http.get(`${BASE_URL_CONSOLIDADO}/consolidado/${DATA_CACHE_HIT}`, authHeaders(data.token));
    check(res, { 'status 200': (r) => r.status === 200 });
}

export function requisitarCacheMiss(data) {
    // Datas espalhadas num intervalo bem distante (1901-1950) - nunca terão saldo, então cada
    // chamada é sempre um miss real de cache (e a resposta 404 confirma isso: só é cacheado
    // quando o repositório encontra um saldo).
    const ano = 1901 + Math.floor(Math.random() * 50);
    const mes = 1 + Math.floor(Math.random() * 12);
    const dia = 1 + Math.floor(Math.random() * 28);
    const dataAleatoria = `${ano}-${String(mes).padStart(2, '0')}-${String(dia).padStart(2, '0')}`;

    const res = http.get(`${BASE_URL_CONSOLIDADO}/consolidado/${dataAleatoria}`, authHeaders(data.token));
    check(res, { 'status 404 (esperado - sem saldo, nunca em cache)': (r) => r.status === 404 });
}
