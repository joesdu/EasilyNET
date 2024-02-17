import http from 'k6/http';
import { check } from 'k6';

export const options = {
    stages: [
        { duration: '30s', target: 20 },
        { duration: '1m30s', target: 10 },
        { duration: '20s', target: 0 },
    ],
};

export default function () {
    const res = http.post('http://localhost:5046/api/MongoTest/MongoPost');
    check(res, { 'status was 200': (r) => r.status == 200 });
}