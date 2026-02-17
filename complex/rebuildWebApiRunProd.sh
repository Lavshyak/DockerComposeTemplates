docker container stop solutionname-main_server_prod-1
docker container rm solutionname-main_server_prod-1
docker image rm solutionname.webapi

./runprod.sh

echo "end of script."

