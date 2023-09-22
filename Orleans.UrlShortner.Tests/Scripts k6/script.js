import http from 'k6/http';
import { sleep } from 'k6';

export default function () {
    http.post('https://localhost:7021/shorten', JSON.stringify({
        'Url': 'https://www.google.it',
        'IsOneShot': true,
        'DurationInSeconds': 20
    }), {
        headers: { 'Content-Type': 'application/json' },
    });
    sleep(1);
    http.get('https://localhost:7021/statistics');
}