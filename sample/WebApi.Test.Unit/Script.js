import { check, sleep } from "k6";
import http from "k6/http";

export const options = {
    stages: [
        { duration: "60s", target: 100 }, // Ramp-up to 100 users over 30s
        { duration: "60s", target: 200 }, // Ramp-up to 200 users over 1 minute
        { duration: "60s", target: 300 }, // Ramp-up to 300 users over 1 minute
        { duration: "60s", target: 500 }, // Ramp-down to 0 users over 1 minute
    ],
};

export default function () {
    const url = 'http://localhost:8848/api/MongoLock/BusinessTest';

    const payload = JSON.stringify({
        email: 'aaa',
        password: 'bbb'
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const res = http.post(url, params);
    check(res, { "status was 200": (r) => r.status == 200 });

    // Optional: Add a small sleep to simulate real user behavior
    sleep(1);
}
