#!/bin/bash
rm -rf dist
mkdir dist
cp *.js dist
cp *.json dist
cp -r templates dist
tar -cf dist.tar dist
rm -rf dist
