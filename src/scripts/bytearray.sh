#!/bin/bash

echo $1 | sed -E 's/[0-9|a-f]{2}/0x&/g' | sed -E 's/ /, /g'