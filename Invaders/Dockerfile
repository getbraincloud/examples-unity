# Use a Linux base image
FROM ubuntu:latest

# Set the working directory
WORKDIR /app

# Copy your Unity server build files to the container
COPY . .

# Install ca-certificates to fix SSL certificate issues
RUN apt-get update && apt-get install -y ca-certificates && apt-get clean && rm -rf /var/lib/apt/lists/*

# Expose the ports your server listens on
# Replace 7777 with the actual port your server listens on
EXPOSE 7777/tcp
EXPOSE 7777/udp
EXPOSE 7777

# Command to run your server executable
CMD ["./Invaders.x86_64"]