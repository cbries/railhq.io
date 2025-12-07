#!/bin/bash

sudo docker rm -f redis-local || true
sudo docker compose -f docker-compose-redis.yml up -d

