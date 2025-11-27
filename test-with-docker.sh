set -e

cd "$(dirname "$0")/backend"

docker build -f ./src/Order.Worker/Dockerfile -t order-worker:tests .
docker build -f ./src/Order.Api/Dockerfile -t order-api:tests .

dotnet test
