package main

import (
	"context"
	"encoding/json"
	"io/ioutil"
	"log"
	"main/otelx"
	"net/http"
	"os"
	"strconv"
	"strings"
	"time"

	dapr "github.com/dapr/go-sdk/client"
	"github.com/gin-gonic/gin"
	"go.opentelemetry.io/contrib/instrumentation/github.com/gin-gonic/gin/otelgin"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/metric"
)

var db = make(map[string]string)

var (
	name                  = os.Getenv("OTEL_SERVICE_NAME")
	isInsecure            bool
	otelTarget            string
	headers               map[string]string
	meter                 metric.Meter
	metricRequestTotal    metric.Int64Counter
	responseTimeHistogram metric.Int64Histogram
)

func setupRouter() *gin.Engine {
	// Disable Console Color
	// gin.DisableConsoleColor()

	r := gin.Default()
	r.Use(otelgin.Middleware(name))
	r.Use(monitorInterceptor())

	// Ping test
	r.GET("/ping", func(c *gin.Context) {
		c.String(http.StatusOK, "pong")
	})

	r.POST("api/create-order", func(c *gin.Context) {
		// Parse JSON
		var order struct {
			CustomerName string `json:"CustomerName" binding:"required"`
			ItemID       string `json:"ItemID" binding:"required"`
			Quantity     int    `json:"Quantity" binding:"required"`
			Status       string `json:"Status" binding:"required"`
			OrderDate    string `json:"OrderDate" binding:"required"`
			LastUpdated  string `json:"LastUpdated" binding:"required"`
		}

		if err := c.ShouldBindJSON(&order); err != nil {
			c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
			return
		}

		order.OrderDate = time.Now().Format(time.RFC3339)
		order.LastUpdated = time.Now().Format(time.RFC3339)

		client := &http.Client{}
		reqBody, err := json.Marshal(order)
		if err != nil {
			c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to marshal order data"})
			return
		}

		req, err := http.NewRequest("POST", os.Getenv("services__dab__http__0")+"/api/Orders", strings.NewReader(string(reqBody)))
		if err != nil {
			c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to create request"})
			return
		}
		req.Header.Set("Content-Type", "application/json")

		resp, err := client.Do(req)
		if err != nil {
			c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to send request"})
			return
		}
		defer resp.Body.Close()

		if resp.StatusCode != http.StatusCreated {
			c.JSON(resp.StatusCode, gin.H{"error": "Failed to create order"})
			return
		}

		c.JSON(http.StatusCreated, gin.H{"status": "order created"})

		daprClient, err := dapr.NewClient()
		if err != nil {
			panic(err)
		}
		defer daprClient.Close()
		//Using Dapr SDK to publish a topic
		body, err := ioutil.ReadAll(resp.Body)
		if err != nil {
			return
		}

		var response struct {
			Value []struct {
				OrderID int `json:"OrderID"`
			} `json:"value"`
		}

		if err := json.Unmarshal(body, &response); err != nil {
			return
		}

		if len(response.Value) == 0 {
			return
		}

		orderID := strconv.Itoa(response.Value[0].OrderID)
		log.Printf("Order ID: %s", orderID)
		ctx := context.Background()
		payload := map[string]string{"OrderID": orderID}
		payloadBytes, err := json.Marshal(payload)
		if err != nil {
			c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to marshal payload"})
			return
		}
		if err := daprClient.PublishEvent(ctx, "pubsub", "process-payment", payloadBytes); err != nil {
			panic(err)
		}
	})

	return r
}

func main() {
	otelEndpoint := strings.Split(os.Getenv("OTEL_EXPORTER_OTLP_ENDPOINT"), "https://")
	if len(otelEndpoint) > 1 {
		isInsecure = false
		otelTarget = otelEndpoint[1]
	} else {
		isInsecure = true
		otelTarget = strings.Split(os.Getenv("OTEL_EXPORTER_OTLP_ENDPOINT"), "http://")[1]
	}
	otelHeaders := strings.Split(os.Getenv("OTEL_EXPORTER_OTLP_HEADERS"), "=")
	if len(otelHeaders) > 1 {
		headers = map[string]string{otelHeaders[0]: otelHeaders[1]}
	}
	// Initialize OpenTelemetry
	err := otelx.SetupOTelSDK(context.Background(), otelTarget, isInsecure, headers, name)
	if err != nil {
		log.Printf("Failed to initialize OpenTelemetry: %v", err)
		return
	}
	defer func() {
		err = otelx.Shutdown(context.Background())
		if err != nil {
			log.Printf("Failed to shutdown OpenTelemetry: %v", err)
		}
	}()

	// Create a tracer and a meter
	meter = otel.Meter(name)
	initGinMetrics()

	r := setupRouter()

	// Listen and Server in 0.0.0.0:PORT
	r.Run(":" + os.Getenv("PORT"))
}

func initGinMetrics() {

	metricRequestTotal, _ = meter.Int64Counter("gin_request_total",
		metric.WithDescription("all the server received request num."),
	)

	// Create a histogram to measure response time
	responseTimeHistogram, _ = meter.Int64Histogram("gin_response_time",
		metric.WithDescription("The distribution of response times."),
	)
}

// monitorInterceptor as gin monitor middleware.
func monitorInterceptor() gin.HandlerFunc {
	return func(c *gin.Context) {
		startTime := time.Now()

		// execute normal process.
		c.Next()

		// after request
		ginMetricHandle(c.Request.Context(), startTime)
	}
}

func ginMetricHandle(c context.Context, start time.Time) {
	// set request total
	metricRequestTotal.Add(c, 1)

	// Record the response time
	duration := time.Since(start)
	responseTimeHistogram.Record(c, duration.Milliseconds())
}
