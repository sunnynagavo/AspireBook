# Use the official Golang image as the base image
FROM golang:1.23

# Set the working directory inside the container
WORKDIR /app

# Copy go.mod and go.sum files
COPY go.mod go.sum ./

# Download dependencies
RUN go mod download

# Copy the source code into the container
COPY . .

# Build the application
RUN go build -o main .

# Set the PORT environment variable
ENV PORT=8000

# Expose port 8000
EXPOSE 8000

# Command to run the application
CMD ["./main"]